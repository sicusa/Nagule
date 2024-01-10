namespace Nagule;

using Microsoft.Extensions.Logging;
using Sia;

public class SimulationFramer : ParallelFramer
{
    public Scheduler Scheduler { get; } = new();

    protected override ILogger CreateLogger(World world, LogLibrary logLib)
        => logLib.Create<SimulationFramer>();

    protected override void OnTick()
    {
        base.OnTick();
        Scheduler.Tick();
    }
}