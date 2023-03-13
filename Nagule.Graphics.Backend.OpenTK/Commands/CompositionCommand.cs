namespace Nagule.Graphics.Backend.OpenTK;

public class CompositionCommand : Command<CompositionCommand, RenderTarget>
{
    public ICommand? Command;

    public override uint? Id => Command?.Id;

    public override void Execute(ICommandHost host)
    {
        host.SendCommand<CompositionTarget>(Command!);
    }
}

public static class CompositionCommandExtensions
{
    public static void SendCompositionCommandBatched(this ICommandBus commandBus, ICommand command)
    {
        var cmd = CompositionCommand.Create();
        cmd.Command = command;
        commandBus.SendCommandBatched(cmd);
    }
}