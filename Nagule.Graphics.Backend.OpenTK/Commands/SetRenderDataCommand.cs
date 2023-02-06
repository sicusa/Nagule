namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class SetRenderDataCommand<TRenderData>
    : Command<SetRenderDataCommand<TRenderData>, RenderTarget>
    where TRenderData : IComponent, new()
{
    public Guid RenderDataId;
    public TRenderData? RenderData;
    public CancellationToken Token;
    public Action<Guid, TRenderData>? CancelCallback;
    public GLSync Sync;

    public override void Execute(ICommandHost host)
    {
        GLHelper.WaitSync(Sync);
        if (Token.IsCancellationRequested) {
            CancelCallback?.Invoke(RenderDataId, RenderData!);
            return;
        }
        host.Set<TRenderData>(RenderDataId, RenderData!);
    }
}

public static class SetRenderDataExtensions
{
    public static void SendRenderData<TRenderData>(
        this ICommandBus commandBus, Guid id, in TRenderData renderData,
        CancellationToken token, Action<Guid, TRenderData> cancelCallback)
        where TRenderData : IComponent, new()
    {
        var cmd = SetRenderDataCommand<TRenderData>.Create();
        cmd.RenderDataId = id;
        cmd.RenderData = renderData;
        cmd.Token = token;
        cmd.CancelCallback = cancelCallback;
        GLHelper.FenceSync(ref cmd.Sync);
        GL.Flush();
        commandBus.SendCommandBatched(cmd);
    }
}