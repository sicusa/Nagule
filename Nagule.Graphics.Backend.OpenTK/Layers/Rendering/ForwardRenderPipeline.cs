namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        ref var cameraData = ref context.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var renderSettings = ref context.RequireOrNullRef<RenderSettingsData>(cameraData.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        ref var pipelineData = ref context.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipelineData)) { return; }

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

        ref var defaultTexData = ref context.RequireOrNullRef<TextureData>(Graphics.DefaultTextureId);
        if (Unsafe.IsNullRef(ref defaultTexData)) { return; }

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
            ref var occluderCullProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.OccluderCullingShaderProgramId);
            if (Unsafe.IsNullRef(ref occluderCullProgram)) {
                goto SkipOccluders;
            }

            GL.UseProgram(occluderCullProgram.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Cull(context, id, in meshData);
            }

            GL.Disable(EnableCap.RasterizerDiscard);

            // render depth buffer with occluders

            GL.ColorMask(false, false, false, false);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                RenderDepth(context, id, in meshData, in pipelineData);
            }

            GL.ColorMask(true, true, true, true);
        }

    SkipOccluders:

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        ref var hizProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.HierarchicalZShaderProgramId);
        if (Unsafe.IsNullRef(ref hizProgram)) { return; }
        GL.UseProgram(hizProgram.Handle);

        // downsample depth buffer to hi-Z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipelineData.HiZTextureHandle, 0);
        
        GL.Viewport(0, 0, pipelineData.HiZWidth, pipelineData.HiZHeight);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        // generate hi-Z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.HiZTextureHandle);

        int width = pipelineData.HiZWidth;
        int height = pipelineData.HiZHeight;
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
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipelineData.HiZTextureHandle, i);
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

        ref var cullProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.CullingShaderProgramId);
        if (Unsafe.IsNullRef(ref cullProgram)) { return; }

        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.HiZTextureHandle);

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
            switch (meshData.RenderMode) {
            case RenderMode.Transparent:
                _transparentMeshes.Add(id);
                continue;
            case RenderMode.Multiplicative:
            case RenderMode.Additive:
                _blendingMeshes.Add(id);
                continue;
            default:
                Render(context, id, in meshData, in pipelineData);
                break;
            }
        }

        // render skybox

        if (renderSettings.SkyboxId != null) {
            ref var skyboxData = ref context.RequireOrNullRef<CubemapData>(renderSettings.SkyboxId.Value);
            if (Unsafe.IsNullRef(ref skyboxData)) {
                goto SkipSkybox;
            }
            ref var skyboxProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.SkyboxShaderProgramId);
            if (Unsafe.IsNullRef(ref skyboxProgram)) {
                goto SkipSkybox;
            }

            GL.UseProgram(skyboxProgram.Handle);
            GL.DepthMask(false);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxData.Handle);
            GL.Uniform1i(skyboxProgram.TextureLocations!["SkyboxTex"], 0);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.DepthMask(true);
        }

    SkipSkybox:

        // render transparent objects

        if (_transparentMeshes.Count != 0) {
            ref var composeProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.TransparencyComposeShaderProgramId);
            if (Unsafe.IsNullRef(ref composeProgram)) {
                goto SkipTransparency;
            }

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

            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthFunc(DepthFunction.Always);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(composeProgram.TextureLocations!["AccumTex"], 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(composeProgram.TextureLocations["RevealTex"], 1);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentMeshes.Clear();
        }

    SkipTransparency:

        // render blending objects

        if (_blendingMeshes.Count != 0) {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

            GL.Enable(EnableCap.Blend);
            GL.DepthMask(false);

            foreach (var id in _blendingMeshes) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                if (meshData.RenderMode == RenderMode.Additive) {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                }
                else {
                    // meshData.RenderMode == RenderMode.Multiplicative
                    GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                }
                Render(context, id, in meshData, in pipelineData);
            }
            _blendingMeshes.Clear();

            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }

        GL.BindVertexArray(VertexArrayHandle.Zero);

        // send post-process command

        var cmd = PostProcessCommand.Create();
        cmd.Sender = this;
        cmd.CameraId = cameraId;
        context.SendCommand(cmd);
    }

    private void PostProcess(ICommandContext context, Guid cameraId)
    {
        ref var cameraData = ref context.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var renderSettings = ref context.RequireOrNullRef<RenderSettingsData>(cameraData.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        ref var pipelineData = ref context.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipelineData)) { return; }

        if (cameraData.RenderTextureId == null) {
            GL.Viewport(0, 0, _windowWidth, _windowHeight);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
        }
        else {
            ref var renderTextureData = ref context.RequireOrNullRef<RenderTextureData>(cameraData.RenderTextureId.Value);
            if (Unsafe.IsNullRef(ref renderTextureData)) { return; }
            GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTextureData.FramebufferHandle);
        }

        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.ColorTextureHandle);

        if (context.TryGet<CameraRenderDebug>(cameraId, out var debug)) {
            ref var postProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.DebugPostProcessingShaderProgramId);
            if (Unsafe.IsNullRef(ref postProgram)) { return; }

            var textures = postProgram.TextureLocations!;
            GL.UseProgram(postProgram.Handle);

            GL.Uniform1i(postProgram.LightsBufferLocation, (int)TextureType.Unknown + 2);
            GL.Uniform1i(postProgram.ClustersBufferLocation, (int)TextureType.Unknown + 3);
            GL.Uniform1i(postProgram.ClusterLightCountsBufferLocation, (int)TextureType.Unknown + 4);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(textures["TransparencyAccumBuffer"], 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(textures["TransparencyRevealBuffer"], 2);

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
            ref var postProgram = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.PostProcessingShaderProgramId);
            if (Unsafe.IsNullRef(ref postProgram)) { return; }

            var customLocations = postProgram.Parameters;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    private void Cull(ICommandContext context, Guid id, in MeshData meshData)
    {
        ref var state = ref context.RequireOrNullRef<MeshRenderState>(id);
        if (Unsafe.IsNullRef(ref state)) { return; }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshData.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, meshData.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BindVertexArray(meshData.CullingVertexArrayHandle);

        GL.BeginTransformFeedback(PrimitiveType.Points);
        GL.BeginQuery(QueryTarget.PrimitivesGenerated, meshData.CulledQueryHandle);
        GL.DrawArrays(PrimitiveType.Points, 0, state.InstanceCount);
        GL.EndQuery(QueryTarget.PrimitivesGenerated);
        GL.EndTransformFeedback();
    }

    private void Render(ICommandContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount == 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref context.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref context.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyMaterial(context, matId, in materialData, in pipeline);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void RenderDepth(ICommandContext context, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount > 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref context.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref context.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyDepthMaterial(context, matId, in materialData, in pipeline);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void ApplyMaterial(ICommandContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref var programData = ref context.RequireOrNullRef<GLSLProgramData>(materialData.ShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(context, in materialData, in programData);
    }

    private void ApplyDepthMaterial(ICommandContext context, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref var programData = ref context.RequireOrNullRef<GLSLProgramData>(materialData.DepthShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref context.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultDepthShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(context, in materialData, in programData);
    }

    private void EnableBuiltInBuffers(in GLSLProgramData programData)
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

    private void EnableTextures(ICommandContext context, in MaterialData materialData, in GLSLProgramData programData)
    {
        var textures = materialData.Textures;
        var textureLocations = programData.TextureLocations;

        if (textures == null || textureLocations == null) {
            return;
        }

        uint texUnitIndex = 1;

        foreach (var (name, texId) in textures) {
            if (!textureLocations.TryGetValue(name, out var location)) {
                continue;
            }

            ref var texData = ref context.RequireOrNullRef<TextureData>(texId);
            if (Unsafe.IsNullRef(ref texData)) {
                GL.Uniform1i(location, 0);
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + texUnitIndex);
            GL.BindTexture(TextureTarget.Texture2d, texData.Handle);
            GL.Uniform1i(location, (int)texUnitIndex);
        }
    }
}