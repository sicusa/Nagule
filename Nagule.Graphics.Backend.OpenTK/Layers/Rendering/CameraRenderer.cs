namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using Aeco;

using Nagule;
using Nagule.Graphics;

public class CameraRenderer : Layer, IEngineUpdateListener, IWindowResizeListener
{
    private class RenderCommand : Command<RenderCommand, RenderTarget>
    {
        public CameraRenderer? Sender;
        public override Guid? Id { get; } = Guid.Empty;

        public override void Execute(ICommandHost host)
        {
            var cameraGroup = Sender!._cameraGroup;
            if (host.Remove<CameraGroupDirty>()) {
                cameraGroup.Refresh(host);
            }
            foreach (var id in cameraGroup) {
                Sender!.RenderToCamera(host, id);
            }
        }
    }

    private class CompositeCommand : Command<CompositeCommand, CompositionTarget>
    {
        public CameraRenderer? Sender;
        public IRenderPipeline? RenderPipeline;
        public ICompositionPipeline? CompositionPipeline;
        public Guid? RenderTextureId;

        public override Guid? Id {
            get {
                if (_id.HasValue) {
                    return _id.Value;
                }
                _id = RenderTextureId.HasValue
                    ? GuidHelper.Merge(
                        RenderPipeline!.RenderSettingsId, RenderTextureId.Value)
                    : RenderPipeline!.RenderSettingsId;
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
            ref var renderSettingsData = ref host.RequireOrNullRef<RenderSettingsData>(RenderPipeline!.RenderSettingsId);
            if (Unsafe.IsNullRef(ref renderSettingsData)) { return; }

            if (RenderTextureId != null) {
                ref var renderTextureData = ref host.RequireOrNullRef<RenderTextureData>(RenderTextureId.Value);
                if (Unsafe.IsNullRef(ref renderTextureData)) { return; }
                
                GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
                CompositionPipeline!.Execute(host, RenderPipeline!, renderTextureData.FramebufferHandle);
            }
            else {
                GL.Viewport(0, 0, Sender!._windowWidth, Sender._windowHeight);
                CompositionPipeline!.Execute(host, RenderPipeline!, FramebufferHandle.Zero);
            }
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
        var cmd = RenderCommand.Create();
        cmd.Sender = this;
        context.SendCommandBatched(cmd);
    }

    public void RenderToCamera(ICommandHost host, Guid cameraId)
    {
        if (host.Remove<MeshGroupDirty>()) {
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
        pipeline.Execute(host, cameraId, _meshGroup);

        if (renderSettingsData.CompositionPipeline != null) {
            var cmd = CompositeCommand.Create();
            cmd.Sender = this;
            cmd.RenderPipeline = pipeline;
            cmd.CompositionPipeline = renderSettingsData.CompositionPipeline;
            cmd.RenderTextureId = cameraData.RenderTextureId;
            host.SendCommand(cmd);
        }
    }
}