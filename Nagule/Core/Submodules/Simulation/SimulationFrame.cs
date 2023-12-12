namespace Nagule;

using Sia;

public class SimulationFrame : Frame
{
    public Scheduler Scheduler { get; } = new();

    public void Start(Func<bool> action)
        => Scheduler.CreateTask(action);

    protected override void OnTick()
    {
        Scheduler.Tick();
    }
}