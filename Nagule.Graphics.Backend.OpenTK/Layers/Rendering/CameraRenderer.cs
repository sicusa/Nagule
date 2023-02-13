namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using Aeco;

using Nagule;
using Nagule.Graphics;

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

    private class CompositeCommand : Command<CompositeCommand, CompositionTarget>
    {
        public ForwardRenderPipeline? Sender;
        public Guid RenderSettingsId;
        public Guid? RenderTextureId;

        public override Guid? Id {
            get {
                if (_id.HasValue) {
                    return _id.Value;
                }
                _id = RenderTextureId.HasValue
                    ? GuidHelper.Merge(RenderSettingsId, RenderTextureId.Value)
                    : RenderSettingsId;
                return _id;
            }
        }

        private Guid? _id;

        public override void Dispose()
        {
            base.Dispose();
            _id = null;
        }

        public override void Execute(ICommandHost host)
        {
            ref var renderSettingsData = ref host.RequireOrNullRef<RenderSettingsData>(RenderSettingsId);
            if (Unsafe.IsNullRef(ref renderSettingsData)) { return; }

            if (RenderTextureId != null) {
                ref var renderTextureData = ref host.RequireOrNullRef<RenderTextureData>(RenderTextureId.Value);
                if (Unsafe.IsNullRef(ref renderTextureData)) { return; }
                
                GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTextureData.FramebufferHandle);
            }
            else {
                GL.Viewport(0, 0, Sender!._windowWidth, Sender._windowHeight);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
            }

            Blit(host, renderSettingsData.RenderPipeline.ColorTextureHandle);
        }

        private void Blit(ICommandHost host, TextureHandle texture)
        {
            ref var postProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.PostProcessingShaderProgramId);
            if (Unsafe.IsNullRef(ref postProgram)) { return; }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, texture);

            var customLocations = postProgram.Parameters;
            GL.UseProgram(postProgram.Handle);

            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            GL.Enable(EnableCap.DepthTest);

            GL.BindVertexArray(VertexArrayHandle.Zero);
        }
    }

    private CameraGroup _cameraGroup = new();
    private MeshGroup _meshGroup = new();

    private int _windowWidth;
    private int _windowHeight;

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
        if (host.Remove<MeshDataDirty>(MeshDataDirty.Id)) {
            _meshGroup.Refresh(host);
        }

        ref var cameraData = ref host.RequireOrNullRef<CameraData>(cameraId);
        if (Unsafe.IsNullRef(ref cameraData)) { return; }

        ref var renderSettingsData = ref host.RequireOrNullRef<RenderSettingsData>(cameraData.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettingsData)) { return; }

        var pipeline = renderSettingsData.RenderPipeline;

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraData.Handle);
        GL.Viewport(0, 0, pipeline.Width, pipeline.Height);

        GLHelper.Clear(cameraData.ClearFlags);
        pipeline.Render(host, _meshGroup);

        if (renderSettingsData.IsCompositionEnabled) {
            var cmd = CompositeCommand.Create();
            cmd.Sender = this;
            cmd.RenderSettingsId = cameraData.RenderSettingsId;
            cmd.RenderTextureId = cameraData.RenderTextureId;
            host.SendCommand(cmd);
        }
    }
}