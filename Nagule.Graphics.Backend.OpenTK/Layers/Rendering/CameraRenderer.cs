namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using Aeco;

using Nagule;
using Nagule.Graphics;

public class CameraRenderer : Layer, IEngineUpdateListener
{
    private class RenderCommand : Command<RenderCommand, RenderTarget>
    {
        public CameraRenderer? Sender;
        public override uint? Id { get; } = 0;

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
        public IRenderPipeline? RenderPipeline;
        public ICompositionPipeline? CompositionPipeline;
        public uint CameraId;

        public override uint? Id => RenderPipeline!.RenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            CompositionPipeline!.Execute(host, CameraId, RenderPipeline!);
        }
    }

    private CameraGroup _cameraGroup = new();
    private MeshGroup _meshGroup = new();

    public void OnEngineUpdate(IContext context)
    {
        var cmd = RenderCommand.Create();
        cmd.Sender = this;
        context.SendCommandBatched(cmd);
    }

    public void RenderToCamera(ICommandHost host, uint cameraId)
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
            cmd.CameraId = cameraId;
            cmd.RenderPipeline = pipeline;
            cmd.CompositionPipeline = renderSettingsData.CompositionPipeline;
            host.SendCommand(cmd);
        }
    }
}