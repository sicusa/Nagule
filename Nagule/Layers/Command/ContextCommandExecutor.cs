namespace Nagule;

using Aeco;

public class ContextCommandExecutor : Layer, IEngineUpdateListener
{
    private CommandRecorder _recorder = new("ContextCommands");

    public void OnEngineUpdate(IContext context)
    {
        while (context.TryGetCommand<ContextTarget>(out var command)) {
            _recorder.Record(command);
        }
        _recorder.Execute(context);
    }
}