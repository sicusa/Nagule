namespace Nagule.Graphics;

using Microsoft.Extensions.Logging;
using Sia;

public class RenderFramer : ParallelFramer
{
    public World World { get; } = new World();

    protected override ILogger CreateLogger(World world, LogLibrary logLib)
        => logLib.Create<RenderFramer>();
}