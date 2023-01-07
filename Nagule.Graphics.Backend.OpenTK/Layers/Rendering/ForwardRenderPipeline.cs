namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;

using PrimitiveType = global::OpenTK.Graphics.OpenGL.PrimitiveType;

public class ForwardRenderPipeline : VirtualLayer, ILoadListener, IEngineUpdateListener, ILateUpdateListener, IWindowResizeListener
{
    private class RenderCommand : Command<RenderCommand>
    {
        public ForwardRenderPipeline? Sender;
        public Guid CameraId;

        public override void Execute(IContext context)
        {
            Sender!.RenderToCamera(context, CameraId);
        }
    }

    private class PostProcessCommand : Command<PostProcessCommand>
    {
        public ForwardRenderPipeline? Sender;
        public Guid CameraId;
        public GLSync Sync;

        public override void Execute(IContext context)
        {
            GLHelper.WaitSync(Sync);
            Sender!.PostProcess(context, CameraId);
        }
    }

    private class CameraGroup : Group<Resource<Camera>>
    {
        public override void Refresh(IDataLayer<IComponent> dataLayer)
        {
            Reset(dataLayer, dataLayer.Query<Resource<Camera>>()
                .OrderBy(id => dataLayer.Inspect<CameraData>(id).Depth));
        }
    }

    private CameraGroup _cameraGroup = new();
    private Group<Resource<Mesh>> _meshGroup = new();
    private Group<Occluder, Resource<Mesh>> _occluderGroup = new();
    private List<Guid> _delayedMeshes = new();
    private List<Guid> _transparentMeshes = new();

    private int _windowWidth;
    private int _windowHeight;
    private VertexArrayHandle _defaultVertexArray;
    private float[] _transparencyClearColor = {0, 0, 0, 1};

    public void OnLoad(IContext context)
    {
        _defaultVertexArray = GL.GenVertexArray();
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void OnEngineUpdate(IContext context)
    {
        if (context.Frame <= 2) { return; }

        foreach (var id in _cameraGroup.Query(context)) {
            var cmd = RenderCommand.Create();
            cmd.Sender = this;
            cmd.CameraId = id;
            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }

    public void OnLateUpdate(IContext context)
    {
        _meshGroup.Query(context);
        _occluderGroup.Query(context);
    }

    public void RenderToCamera(IContext context, Guid cameraId)
    {
        ref readonly var cameraData = ref context.Inspect<CameraData>(cameraId);
        ref readonly var pipelineData = ref context.Inspect<RenderPipelineData>(cameraData.RenderPipelineId);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipelineData.ColorFramebufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, pipelineData.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraData.Handle);
        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

        var lightBufferHandle = context.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 1);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref context.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);

        ref readonly var defaultTexData = ref context.Inspect<TextureData>(Graphics.DefaultTextureId);

        bool valid;

        // set viewport && clear buffers

        GL.Viewport(0, 0, pipelineData.Width, pipelineData.Height);

        switch (cameraData.ClearFlags) {
        case ClearFlags.Color | ClearFlags.Depth:
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            break;
        case ClearFlags.Color:
            GL.Clear(ClearBufferMask.ColorBufferBit);
            break;
        case ClearFlags.Depth:
            GL.Clear(ClearBufferMask.DepthBufferBit);
            break;
        }

        // generate early z-buffer with occluder meshes

        if (_occluderGroup.Count != 0) {
            // cull occluders by camera frustum

            ref readonly var occluderCullProgram =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.OccluderCullingShaderProgramId, out valid);
            if (!valid) {
                goto SkipOccluders;
            }

            GL.UseProgram(occluderCullProgram.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

            foreach (var id in _meshGroup) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Cull(context, id, in meshData);
            }

            GL.Disable(EnableCap.RasterizerDiscard);

            // render depth buffer with occluders

            GL.ColorMask(false, false, false, false);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                RenderBlank(context, id, in meshData, in pipelineData);
            }

            GL.ColorMask(true, true, true, true);
        }

    SkipOccluders:

        // generate hierarchical-Z buffer

        ref readonly var hizProgram =
            ref context.InspectValidGraphics<ShaderProgramData>(
                Graphics.HierarchicalZShaderProgramId, out valid);
        if (!valid) { return; }

        GL.UseProgram(hizProgram.Handle);

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

        int width = pipelineData.Width;
        int height = pipelineData.Height;
        int levelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        for (int i = 1; i < levelCount; ++i) {
            width /= 2;
            height /= 2;
            width = width > 0 ? width : 1;
            height = height > 0 ? height : 1;
            GL.Viewport(0, 0, width, height);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, i - 1);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, i - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipelineData.DepthTextureHandle, i);
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipelineData.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, pipelineData.Width, pipelineData.Height);

        // cull instances by camera frustum and occlusion

        ref readonly var cullProgram =
            ref context.InspectValidGraphics<ShaderProgramData>(
                Graphics.CullingShaderProgramId, out valid);
        if (!valid) { return; }

        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

        foreach (var id in _meshGroup) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            if (meshData.IsOccluder) {
                continue;
            }
            Cull(context, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);

        // render opaque meshes

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

        foreach (var id in _meshGroup) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            if (meshData.RenderMode == RenderMode.Transparent ||
                    meshData.RenderMode == RenderMode.UnlitTransparent) {
                _transparentMeshes.Add(id);
                continue;
            }
            if (meshData.RenderMode != RenderMode.Opaque && meshData.RenderMode != RenderMode.Cutoff &&
                    meshData.RenderMode != RenderMode.Unlit && meshData.RenderMode != RenderMode.UnlitCutoff) {
                _delayedMeshes.Add(id);
                continue;
            }
            Render(context, id, in meshData, in pipelineData);
        }

        // render transparent objects

        if (_transparentMeshes.Count != 0) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipelineData.TransparencyFramebufferHandle);
            GL.ClearBufferf(Buffer.Color, 0, _transparencyClearColor);
            GL.ClearBufferf(Buffer.Color, 1, _transparencyClearColor);

            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

            foreach (var id in _transparentMeshes) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Render(context, id, in meshData, in pipelineData);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipelineData.ColorFramebufferHandle);

            // compose transparency

            ref readonly var composeProgram =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.TransparencyComposeShaderProgramId, out valid);
            if (!valid) { return; }

            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["AccumTex"].Location, 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["RevealTex"].Location, 1);

            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentMeshes.Clear();
        }

        // render delayed objects

        if (_delayedMeshes.Count != 0) {
            GL.Enable(EnableCap.Blend);
            GL.DepthMask(false);

            foreach (var id in _delayedMeshes) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                if (meshData.RenderMode == RenderMode.Additive || meshData.RenderMode == RenderMode.UnlitAdditive) {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                }
                else {
                    // meshData.RenderMode == RenderMode.Multiplicative || meshData.RenderMode == RenderMode.UnlitMultiplicative
                    GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                }
                Render(context, id, in meshData, in pipelineData);
            }
            _delayedMeshes.Clear();

            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }

        GL.BindVertexArray(VertexArrayHandle.Zero);

        // send post-process command

        var cmd = PostProcessCommand.Create();
        cmd.Sender = this;
        cmd.CameraId = cameraId;
        GLHelper.FenceSync(ref cmd.Sync);
        context.SendCommand<RenderCompositionTarget>(cmd);
    }

    private void PostProcess(IContext context, Guid cameraId)
    {
        ref readonly var cameraData = ref context.Inspect<CameraData>(cameraId);
        ref readonly var pipelineData = ref context.Inspect<RenderPipelineData>(cameraData.RenderPipelineId);

        if (cameraData.RenderTextureId == null) {
            GL.Viewport(0, 0, _windowWidth, _windowHeight);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
        }
        else {
            ref readonly var renderTextureData = ref context.Inspect<RenderTextureData>(cameraData.RenderTextureId.Value);
            GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTextureData.FramebufferHandle);
        }

        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.ColorTextureHandle);

        if (context.TryGet<CameraRenderDebug>(cameraId, out var debug)) {
            ref readonly var postProgram =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.DebugPostProcessingShaderProgramId, out bool valid);
            if (!valid) { return;}

            var parameters = postProgram.CustomParameters;
            GL.UseProgram(postProgram.Handle);

            GL.Uniform1i(postProgram.LightsBufferLocation, (int)TextureType.Unknown + 2);
            GL.Uniform1i(postProgram.ClustersBufferLocation, (int)TextureType.Unknown + 3);
            GL.Uniform1i(postProgram.ClusterLightCountsBufferLocation, (int)TextureType.Unknown + 4);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(parameters["TransparencyAccumBuffer"].Location, 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(parameters["TransparencyRevealBuffer"].Location, 2);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);
            GL.Uniform1i(postProgram.DepthBufferLocation, 3);

            var subroutines = postProgram.SubroutineIndices![Nagule.Graphics.ShaderType.Fragment];
            var subroutineName = debug.DisplayMode switch {
                DisplayMode.TransparencyAccum => "ShowTransparencyAccum",
                DisplayMode.TransparencyAlpha => "ShowTransparencyReveal",
                DisplayMode.Depth => "ShowDepth",
                DisplayMode.Clusters => "ShowClusters",
                _ => "ShowColor"
            };
            uint index = subroutines[subroutineName];
            GL.UniformSubroutinesui(global::OpenTK.Graphics.OpenGL.ShaderType.FragmentShader, 1, index);
        }
        else {
            ref readonly var postProgram =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.PostProcessingShaderProgramId, out bool valid);
            if (!valid) { return; }

            var customLocations = postProgram.CustomParameters;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.Points, 0, 1);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    private void Cull(IContext context, Guid id, in MeshData meshData)
    {
        ref readonly var state = ref context.Inspect<MeshRenderState>(id);

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshData.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, meshData.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BindVertexArray(meshData.CullingVertexArrayHandle);

        GL.BeginTransformFeedback(PrimitiveType.Points);
        GL.BeginQuery(QueryTarget.PrimitivesGenerated, meshData.CulledQueryHandle);
        GL.DrawArrays(PrimitiveType.Points, 0, state.MaximumInstanceIndex + 1);
        GL.EndQuery(QueryTarget.PrimitivesGenerated);
        GL.EndTransformFeedback();
    }

    private void Render(IContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        var matId = meshData.MaterialId;

        ref readonly var materialData = ref context.Inspect<MaterialData>(matId);
        ref readonly var state = ref context.Inspect<MeshRenderState>(meshId);

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);

        if (visibleCount > 0) {
            ApplyMaterial(context, matId, in materialData, in pipeline);
            GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);
        }

        bool materialApplied = false;
        foreach (var variantId in state.VariantIds) {
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Object,
                context.Require<MeshRenderableData>(variantId).VariantBufferHandle);
            if (context.TryGet<MaterialData>(variantId, out var overwritingMaterialData)) {
                ApplyMaterial(context, matId, in overwritingMaterialData, in pipeline);
            }
            else if (!materialApplied) {
                ApplyMaterial(context, matId, in materialData, in pipeline);
            }
            GL.DrawElements(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, 0);
        }

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void RenderBlank(IContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        var matId = meshData.MaterialId;

        ref readonly var materialData =
            ref context.InspectValidGraphics<MaterialData>(matId, out bool valid);
        if (!valid) {
            materialData =
                ref context.InspectValidGraphics<MaterialData>(
                    Graphics.DefaultMaterialId, out valid);
            if (!valid) { return; }
        }

        ref readonly var state = ref context.Inspect<MeshRenderState>(meshId);

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);

        if (visibleCount > 0) {
            ApplyMaterialBlank(context, matId, in materialData, in pipeline);
            GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);
        }

        bool materialApplied = false;
        foreach (var variantId in state.VariantIds) {
            GL.BindBufferBase(
                BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Object,
                context.Require<MeshRenderableData>(variantId).VariantBufferHandle);

            if (context.TryGet<MaterialData>(variantId, out var overwritingMaterialData)) {
                ApplyMaterialBlank(context, matId, in overwritingMaterialData, in pipeline);
            }
            else if (!materialApplied) {
                ApplyMaterialBlank(context, matId, in materialData, in pipeline);
            }

            GL.DrawElements(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, 0);
        }

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void ApplyMaterial(IContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref readonly var programData =
            ref context.InspectValidGraphics<ShaderProgramData>(
                materialData.ShaderProgramId, out bool valid);
        if (!valid) {
            programData =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.DefaultOpaqueShaderProgramId, out valid);
            if (!valid) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        const int texCount = (int)TextureType.Unknown;
        for (int i = 0; i != texCount; ++i) {
            EnableTexture(context, i, in materialData, in programData);
        }

        EnableBuiltInBuffers(in programData);
        EnableMaterialCustomParameters(context, id, in programData);
    }

    private void ApplyMaterialBlank(IContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref readonly var programData =
            ref context.InspectValidGraphics<ShaderProgramData>(
                materialData.DepthShaderProgramId, out bool valid);
        if (!valid) {
            programData =
                ref context.InspectValidGraphics<ShaderProgramData>(
                    Graphics.DefaultDepthShaderProgramId, out valid);
            if (!valid) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableTexture(context, TextureType.Height, in materialData, in programData);
        EnableTexture(context, TextureType.Displacement, in materialData, in programData);

        EnableBuiltInBuffers(in programData);
        EnableMaterialCustomParameters(context, id, in programData);
    }

    private void EnableBuiltInBuffers(in ShaderProgramData programData)
    {
        const int texCount = (int)TextureType.Unknown;

        if (programData.DepthBufferLocation != -1) {
            GL.Uniform1i(programData.DepthBufferLocation, texCount + 1);
        }
        if (programData.LightsBufferLocation != -1) {
            GL.Uniform1i(programData.LightsBufferLocation, texCount + 2);
        }
        if (programData.ClustersBufferLocation != -1) {
            GL.Uniform1i(programData.ClustersBufferLocation, texCount + 3);
        }
        if (programData.ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1i(programData.ClusterLightCountsBufferLocation, texCount + 4);
        }
    }

    private void EnableMaterialCustomParameters(IContext context, Guid materialId, in ShaderProgramData programData)
    {
        if (!context.TryGet<MaterialSettings>(materialId, out var settings)) {
            return;
        }
        foreach (var (name, value) in settings.Parameters) {
            if (programData.CustomParameters.TryGetValue(name, out var par)) {
                try {
                    GLHelper.SetUniform(par.Type, par.Location, value);
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to set material parameter '{name}': " + e.Message);
                }
            }
        }
    }

    private void EnableTexture(IContext context, int textureType, in MaterialData materialData, in ShaderProgramData programData)
    {
        var textures = materialData.Textures;
        var textureLocations = programData.TextureLocations;

        int location = textureLocations![textureType];
        if (location == -1) { return; }

        var texId = textures[textureType];
        if (texId == null || !context.Contains<GraphicsResourceValid>(texId.Value)) {
            GL.Uniform1i(location, 0);
            return;
        }

        var textureData = context.Inspect<TextureData>(texId.Value);
        GL.ActiveTexture(TextureUnit.Texture1 + (uint)textureType);
        GL.BindTexture(TextureTarget.Texture2d, textureData.Handle);
        GL.Uniform1i(location, textureType + 1);
    }

    private void EnableTexture(IContext context, TextureType textureType, in MaterialData materialData, in ShaderProgramData programData)
        => EnableTexture(context, (int)textureType, in materialData, in programData);
}