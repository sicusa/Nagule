namespace Nagule;

using Microsoft.Extensions.Logging;
using Sia;

public class SimulationFramer : ParallelFramer
{
    protected override ILogger CreateLogger(World world, LogLibrary logLib)
        => logLib.Create<SimulationFramer>();
}