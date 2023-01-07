namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

public class MarkResourceValidComponent : Command<MarkResourceValidComponent>
{
    public Guid ResourceId;
    public GLSync Sync;

    public override void Execute(IContext context)
    {
        GLHelper.WaitSync(Sync);
        context.Acquire<GraphicsResourceValid>(ResourceId);
    }
}

public static class MarkResourceValidExtensions
{
    public static void SendResourceValidCommand(this IContext context, Guid resourceId)
    {
        var cmd = MarkResourceValidComponent.Create();
        cmd.ResourceId = resourceId;
        GLHelper.FenceSync(ref cmd.Sync);
        GL.Flush();
        context.SendCommand<RenderTarget>(cmd);
    }
}