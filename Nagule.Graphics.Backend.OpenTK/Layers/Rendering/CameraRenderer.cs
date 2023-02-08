namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;

using PrimitiveType = global::OpenTK.Graphics.OpenGL.PrimitiveType;

public class ForwardRenderPipeline : Layer, IEngineUpdateListener, IWindowResizeListener
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

    private class MeshGroup : Group<MeshData>
    {
        private ForwardRenderPipeline _owner;

        public MeshGroup(ForwardRenderPipeline owner)
        {
            _owner = owner;
        }

        public override void Refresh(IReadableDataLayer<IComponent> dataLayer)
        {
            base.Refresh(dataLayer);

            var occluderMeshes = _owner._occluderMeshes;
            var opaqueMeshes = _owner._opaqueMeshes;
            var blendingMeshes = _owner._blendingMeshes;
            var transparentMeshes = _owner._transparentMeshes;

            occluderMeshes.Clear();
            opaqueMeshes.Clear();
            blendingMeshes.Clear();
            transparentMeshes.Clear();

            foreach (var id in this) {
                if (dataLayer.Contains<Occluder>(id)) {
                    occluderMeshes.Add(id);
                    opaqueMeshes.Add(id);
                    continue;
                }

                ref readonly var meshData = ref dataLayer.Inspect<MeshData>(id);
                switch (meshData.RenderMode) {
                case RenderMode.Transparent:
                    transparentMeshes.Add(id);
                    continue;
                case RenderMode.Multiplicative:
                case RenderMode.Additive:
                    blendingMeshes.Add(id);
                    continue;
                }

                opaqueMeshes.Add(id);
            }
        }
    }

    private CameraGroup _cameraGroup = new();
    private MeshGroup _meshGroup;

    private List<Guid> _occluderMeshes = new();
    private List<Guid> _opaqueMeshes = new();
    private List<Guid> _blendingMeshes = new();
    private List<Guid> _transparentMeshes = new();

    private int _windowWidth;
    private int _windowHeight;

    public ForwardRenderPipeline()
    {
        _meshGroup = new(this);
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

        ref var pipeline = ref host.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipeline)) { return; }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipeline.ColorFramebufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, pipeline.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraData.Handle);

        _meshGroup.Query(host);

        GL.Viewport(0, 0, pipeline.Width, pipeline.Height);
        GLHelper.Clear(cameraData.ClearFlags);

        if (_occluderMeshes.Count != 0) {
            GLHelper.CullOccluderMeshes(host, in pipeline, CollectionsMarshal.AsSpan(_occluderMeshes));
            GLHelper.RenderDepth(host, in pipeline, CollectionsMarshal.AsSpan(_occluderMeshes));
        }

        GLHelper.GenerateHiZBuffer(host, in pipeline);
        GLHelper.CullMeshes(host, in pipeline, _meshGroup.AsSpan());
        GLHelper.ActivateBuiltInTextures(host, in pipeline);
        GLHelper.RenderOpaque(host, in pipeline, CollectionsMarshal.AsSpan(_opaqueMeshes));

        if (renderSettings.SkyboxId != null) {
            ref var skyboxData = ref host.RequireOrNullRef<TextureData>(renderSettings.SkyboxId.Value);
            if (!Unsafe.IsNullRef(ref skyboxData)) {
                GLHelper.DrawCubemapSkybox(host, skyboxData.Handle);
            }
        }

        if (_transparentMeshes.Count != 0) {
            GLHelper.RenderTransparent(host, in pipeline, CollectionsMarshal.AsSpan(_transparentMeshes));
        }
        if (_blendingMeshes.Count != 0) {
            GLHelper.RenderBlending(host, in pipeline, CollectionsMarshal.AsSpan(_blendingMeshes));
        }

        var cmd = PostProcessCommand.Create();
        cmd.Sender = this;
        cmd.CameraId = cameraId;
        host.SendCommand(cmd);
    }

    private void PostProcess(ICommandHost host, Guid cameraId)
    {
        ref var cameraData = ref host.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var pipelineData = ref host.RequireOrNullRef<RenderPipelineData>(cameraData.RenderPipelineId);
        if (Unsafe.IsNullRef(ref pipelineData)) { return; }

        ref var postProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.PostProcessingShaderProgramId);
        if (Unsafe.IsNullRef(ref postProgram)) { return; }

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

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipelineData.ColorTextureHandle);

        var customLocations = postProgram.Parameters;
        GL.UseProgram(postProgram.Handle);

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }
}