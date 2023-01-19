namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;

using PrimitiveType = global::OpenTK.Graphics.OpenGL.PrimitiveType;

public class ForwardRenderPipeline : Layer, ILoadListener, IEngineUpdateListener, IWindowResizeListener
{
    private class RenderCommand : Command<RenderCommand, RenderTarget>
    {
        public ForwardRenderPipeline? Sender;
        public Guid CameraId;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandContext context)
        {
            Sender!.RenderToCamera(context, CameraId);
        }
    }

    private class PostProcessCommand : Command<PostProcessCommand, CompositionTarget>
    {
        public ForwardRenderPipeline? Sender;
        public Guid CameraId;

        public override void Execute(ICommandContext context)
        {
            Sender!.PostProcess(context, CameraId);
        }
    }

    private class CameraGroup : Group<Resource<Camera>>
    {
        public override void Refresh(IReadableDataLayer<IComponent> dataLayer)
        {
            Reset(dataLayer, dataLayer.Query<Resource<Camera>>()
                .OrderBy(id => dataLayer.Inspect<Resource<Camera>>(id).Value.Depth));
        }
    }

    private CameraGroup _cameraGroup = new();
    private Group<MeshData> _meshGroup = new();
    private Group<Occluder, MeshData> _occluderGroup = new();

    private List<Guid> _blendingMeshes = new();
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
        foreach (var id in _cameraGroup.Query(context)) {
            var cmd = RenderCommand.Create();
            cmd.Sender = this;
            cmd.CameraId = id;
            context.SendCommandBatched(cmd);
        }
    }

    public void RenderToCamera(ICommandContext context, Guid cameraId)
    {
        if (!context.Contains<CameraData>(cameraId)) {
            return;
        }
        ref readonly var cameraData = ref context.Inspect<CameraData>(cameraId);

        if (!context.Contains<RenderPipelineData>(cameraData.RenderPipelineId)) {
            return;
        }
        ref readonly var pipelineData =
            ref context.Inspect<RenderPipelineData>(cameraData.RenderPipelineId);

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

        if (!context.TryGet<TextureData>(Graphics.DefaultTextureId, out var defaultTexData)) {
            return;
        }

        _meshGroup.Refresh(context);
        _occluderGroup.Refresh(context);

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
            if (!context.TryGet<ShaderProgramData>(
                    Graphics.OccluderCullingShaderProgramId, out var occluderCullProgram)) {
                goto SkipOccluders;
            }

            GL.UseProgram(occluderCullProgram.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

            foreach (var id in _occluderGroup) {
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

        if (!context.TryGet<ShaderProgramData>(
                Graphics.HierarchicalZShaderProgramId, out var hizProgram)) {
            return;
        }

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
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipelineData.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, pipelineData.Width, pipelineData.Height);

        // cull instances by camera frustum and occlusion

        if (!context.TryGet<ShaderProgramData>(
                Graphics.CullingShaderProgramId, out var cullProgram)) {
            return;
        }

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
            if (RenderModeHelper.IsTransparent(meshData.RenderMode)) {
                _transparentMeshes.Add(id);
                continue;
            }
            if (RenderModeHelper.IsBlending(meshData.RenderMode)) {
                _blendingMeshes.Add(id);
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

            if (!context.TryGet<ShaderProgramData>(
                    Graphics.TransparencyComposeShaderProgramId, out var composeProgram)) {
                return;
            }

            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["AccumTex"].Location, 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["RevealTex"].Location, 1);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentMeshes.Clear();
        }

        // render blending objects

        if (_blendingMeshes.Count != 0) {
            GL.Enable(EnableCap.Blend);
            GL.DepthMask(false);

            foreach (var id in _blendingMeshes) {
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
            _blendingMeshes.Clear();

            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }

        GL.BindVertexArray(VertexArrayHandle.Zero);

        // render skybox

        

        // send post-process command

        var cmd = PostProcessCommand.Create();
        cmd.Sender = this;
        cmd.CameraId = cameraId;
        context.SendCommand(cmd);
    }

    private void PostProcess(ICommandContext context, Guid cameraId)
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
            if (!context.TryGet<ShaderProgramData>(
                    Graphics.DebugPostProcessingShaderProgramId, out var postProgram)) {
                return;
            }

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
            if (!context.TryGet<ShaderProgramData>(
                    Graphics.PostProcessingShaderProgramId, out var postProgram)) {
                return;
            }
            var customLocations = postProgram.CustomParameters;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    private void Cull(ICommandContext context, Guid id, in MeshData meshData)
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

    private void Render(ICommandContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        var matId = meshData.MaterialId;

        if (!context.TryGet<MaterialData>(matId, out var materialData)) {
            if (!context.TryGet<MaterialData>(Graphics.DefaultMaterialId, out materialData)) {
                return;
            }
        }

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

    private void RenderBlank(ICommandContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        var matId = meshData.MaterialId;

        if (!context.TryGet<MaterialData>(matId, out var materialData)) {
            if (!context.TryGet<MaterialData>(Graphics.DefaultMaterialId, out materialData)) {
                return;
            }
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

    private void ApplyMaterial(ICommandContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        if (!context.TryGet<ShaderProgramData>(materialData.ShaderProgramId, out var programData)) {
            if (!context.TryGet<ShaderProgramData>(
                    Graphics.DefaultOpaqueShaderProgramId, out programData)) {
                return;
            }
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

    private void ApplyMaterialBlank(ICommandContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        if (!context.TryGet<ShaderProgramData>(materialData.DepthShaderProgramId, out var programData)) {
            if (!context.TryGet<ShaderProgramData>(
                    Graphics.DefaultDepthShaderProgramId, out programData)) {
                return;
            }
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

    private void EnableMaterialCustomParameters(ICommandContext context, Guid materialId, in ShaderProgramData programData)
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

    private void EnableTexture(ICommandContext context, int textureType, in MaterialData materialData, in ShaderProgramData programData)
    {
        var textures = materialData.Textures;
        var textureLocations = programData.TextureLocations;

        int location = textureLocations![textureType];
        if (location == -1) { return; }

        var texId = textures[textureType];
        if (texId == null || !context.TryGet<TextureData>(texId.Value, out var textureData)) {
            GL.Uniform1i(location, 0);
            return;
        }

        GL.ActiveTexture(TextureUnit.Texture1 + (uint)textureType);
        GL.BindTexture(TextureTarget.Texture2d, textureData.Handle);
        GL.Uniform1i(location, textureType + 1);
    }

    private void EnableTexture(ICommandContext context, TextureType textureType, in MaterialData materialData, in ShaderProgramData programData)
        => EnableTexture(context, (int)textureType, in materialData, in programData);
}