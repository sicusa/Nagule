namespace Nagule.Graphics.Backend.OpenTK;

public class MarkResourceInvalidComponent : Command<MarkResourceInvalidComponent>
{
    public Guid ResourceId;

    public override void Execute(IContext context)
    {
        context.Remove<GraphicsResourceValid>(ResourceId);
    }
}

public static class MarkResourceInvalidExtensions
{
    public static void SendResourceInvalidCommand(this IContext context, Guid resourceId)
    {
        var cmd = MarkResourceInvalidComponent.Create();
        cmd.ResourceId = resourceId;
        context.SendCommand<RenderTarget>(cmd);
    }
}