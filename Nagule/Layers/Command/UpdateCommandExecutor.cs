namespace Nagule;

using Aeco;

public class UpdateCommandExecutor : Layer, IEngineUpdateListener
{
    private CommandRecorder _recorder = new();

    public void OnEngineUpdate(IContext context)
    {
        while (context.TryGetCommand<UpdateTarget>(out var command)) {
            _recorder.Record(command);
        }
        _recorder.Execute(context);
    }
}