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

        public override void Execute(ICommandHost host)
        {
            Sender!.RenderToCamera(host, CameraId);
        }
    }

    private class PostProcessCommand : Command<PostProcessCommand, CompositionTarget>
    {
        public ForwardRenderPipeline? Sender;
        public Guid CameraId;

        public override void Execute(ICommandHost host)
        {
            Sender!.PostProcess(host, CameraId);
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

    private const int BuiltInBufferCount = 5;

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

    public void RenderToCamera(ICommandHost host, Guid cameraId)
    {
        ref var cameraData = ref host.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var renderSettings = ref host.RequireOrNullRef<RenderSettingsData>(cameraData.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        ref var pipelineData = ref host.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipelineData)) { return; }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipelineData.ColorFramebufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, pipelineData.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraData.Handle);
        GL.BindVertexArray(_defaultVertexArray);

        ref var defaultTexData = ref host.RequireOrNullRef<TextureData>(Graphics.DefaultTextureId);
        if (Unsafe.IsNullRef(ref defaultTexData)) { return; }

        _meshGroup.Refresh(host);
        _occluderGroup.Refresh(host);

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
            ref var occluderCullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.OccluderCullingShaderProgramId);
            if (Unsafe.IsNullRef(ref occluderCullProgram)) {
                goto SkipOccluders;
            }

            GL.UseProgram(occluderCullProgram.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                Cull(host, id, in meshData);
            }

            GL.Disable(EnableCap.RasterizerDiscard);

            // render depth buffer with occluders

            GL.ColorMask(false, false, false, false);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                RenderDepth(host, id, in meshData, in pipelineData);
            }

            GL.ColorMask(true, true, true, true);
        }

    SkipOccluders:

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        ref var hizProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.HierarchicalZShaderProgramId);
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

        ref var cullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.CullingShaderProgramId);
        if (Unsafe.IsNullRef(ref cullProgram)) { return; }

        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.HiZTextureHandle);

        foreach (var id in _meshGroup) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            if (meshData.IsOccluder) {
                continue;
            }
            Cull(host, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);

        // activate built-in textures

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.DepthTextureHandle);

        var lightBufferHandle = host.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref host.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);

        // render opaque meshes

        foreach (var id in _meshGroup) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            switch (meshData.RenderMode) {
            case RenderMode.Transparent:
                _transparentMeshes.Add(id);
                continue;
            case RenderMode.Multiplicative:
            case RenderMode.Additive:
                _blendingMeshes.Add(id);
                continue;
            }
            Render(host, id, in meshData, in pipelineData);
        }

        // render skybox

        if (renderSettings.SkyboxId != null) {
            ref var skyboxData = ref host.RequireOrNullRef<TextureData>(renderSettings.SkyboxId.Value);
            if (Unsafe.IsNullRef(ref skyboxData)) {
                goto SkipSkybox;
            }
            ref var skyboxProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.SkyboxShaderProgramId);
            if (Unsafe.IsNullRef(ref skyboxProgram)) {
                goto SkipSkybox;
            }

            GL.UseProgram(skyboxProgram.Handle);
            GL.DepthMask(false);

            GL.ActiveTexture(TextureUnit.Texture0 + BuiltInBufferCount);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxData.Handle);
            GL.Uniform1i(skyboxProgram.TextureLocations!["SkyboxTex"], BuiltInBufferCount);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.DepthMask(true);
        }

    SkipSkybox:

        // render transparent objects

        if (_transparentMeshes.Count != 0) {
            ref var composeProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.TransparencyComposeShaderProgramId);
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
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                Render(host, id, in meshData, in pipelineData);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipelineData.ColorFramebufferHandle);

            // compose transparency

            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthFunc(DepthFunction.Always);

            GL.ActiveTexture(TextureUnit.Texture0 + BuiltInBufferCount);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyAccumTextureHandle);
            GL.Uniform1i(composeProgram.TextureLocations!["AccumTex"], BuiltInBufferCount);

            GL.ActiveTexture(TextureUnit.Texture0 + BuiltInBufferCount + 1);
            GL.BindTexture(TextureTarget.Texture2d, pipelineData.TransparencyRevealTextureHandle);
            GL.Uniform1i(composeProgram.TextureLocations["RevealTex"], BuiltInBufferCount + 1);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentMeshes.Clear();
        }

    SkipTransparency:

        // render blending objects

        if (_blendingMeshes.Count != 0) {
            GL.Enable(EnableCap.Blend);
            GL.DepthMask(false);

            foreach (var id in _blendingMeshes) {
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                if (meshData.RenderMode == RenderMode.Additive) {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                }
                else {
                    // meshData.RenderMode == RenderMode.Multiplicative
                    GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                }
                Render(host, id, in meshData, in pipelineData);
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
        host.SendCommand(cmd);
    }

    private void PostProcess(ICommandHost host, Guid cameraId)
    {
        ref var cameraData = ref host.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var renderSettings = ref host.RequireOrNullRef<RenderSettingsData>(cameraData.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        ref var pipelineData = ref host.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipelineData)) { return; }

        if (cameraData.RenderTextureId == null) {
            GL.Viewport(0, 0, _windowWidth, _windowHeight);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
        }
        else {
            ref var renderTextureData = ref host.RequireOrNullRef<RenderTextureData>(cameraData.RenderTextureId.Value);
            if (Unsafe.IsNullRef(ref renderTextureData)) { return; }
            GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTextureData.FramebufferHandle);
        }

        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.ColorTextureHandle);

        if (host.TryGet<CameraRenderDebug>(cameraId, out var debug)) {
            ref var postProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.DebugPostProcessingShaderProgramId);
            if (Unsafe.IsNullRef(ref postProgram)) { return; }

            var textures = postProgram.TextureLocations!;
            GL.UseProgram(postProgram.Handle);

            GL.Uniform1i(postProgram.LightsBufferLocation, 2);
            GL.Uniform1i(postProgram.ClustersBufferLocation, 3);
            GL.Uniform1i(postProgram.ClusterLightCountsBufferLocation, 4);

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
            ref var postProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.PostProcessingShaderProgramId);
            if (Unsafe.IsNullRef(ref postProgram)) { return; }

            var customLocations = postProgram.Parameters;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    private void Cull(ICommandHost host, Guid id, in MeshData meshData)
    {
        ref var state = ref host.RequireOrNullRef<MeshRenderState>(id);
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

    private void Render(ICommandHost host, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount == 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref host.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref host.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyMaterial(host, matId, in materialData, in pipeline);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void RenderDepth(ICommandHost host, Guid meshId, in MeshData meshData, in RenderPipelineData pipeline)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount > 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref host.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref host.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyDepthMaterial(host, matId, in materialData, in pipeline);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    private void ApplyMaterial(ICommandHost host, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref var programData = ref host.RequireOrNullRef<GLSLProgramData>(materialData.ShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(host, in materialData, in programData);
    }

    private void ApplyDepthMaterial(ICommandHost host, Guid id, in MaterialData materialData, in RenderPipelineData pipeline)
    {
        ref var programData = ref host.RequireOrNullRef<GLSLProgramData>(materialData.DepthShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultDepthShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(host, in materialData, in programData);
    }

    private void EnableBuiltInBuffers(in GLSLProgramData programData)
    {
        if (programData.DepthBufferLocation != -1) {
            GL.Uniform1i(programData.DepthBufferLocation, 1);
        }
        if (programData.LightsBufferLocation != -1) {
            GL.Uniform1i(programData.LightsBufferLocation, 2);
        }
        if (programData.ClustersBufferLocation != -1) {
            GL.Uniform1i(programData.ClustersBufferLocation, 3);
        }
        if (programData.ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1i(programData.ClusterLightCountsBufferLocation, 4);
        }
    }

    private void EnableTextures(ICommandHost host, in MaterialData materialData, in GLSLProgramData programData)
    {
        var textures = materialData.Textures;
        var textureLocations = programData.TextureLocations;

        if (textures == null || textureLocations == null) {
            return;
        }

        uint texUnitIndex = BuiltInBufferCount;

        foreach (var (name, texId) in textures) {
            if (!textureLocations.TryGetValue(name, out var location)) {
                continue;
            }

            ref var texData = ref host.RequireOrNullRef<TextureData>(texId);
            if (Unsafe.IsNullRef(ref texData)) {
                GL.Uniform1i(location, 0);
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + texUnitIndex);
            GL.BindTexture(TextureTarget.Texture2d, texData.Handle);
            GL.Uniform1i(location, (int)texUnitIndex);
            
            ++texUnitIndex;
        }
    }
}