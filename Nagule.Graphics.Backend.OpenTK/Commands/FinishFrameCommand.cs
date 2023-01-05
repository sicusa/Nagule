namespace Nagule.Graphics.Backend.OpenTK;

public class FinishFrameCommand : ICommand
{
    public static readonly FinishFrameCommand Instance = new();

    private FinishFrameCommand() {}

    public void Execute(IContext context)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}