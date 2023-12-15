namespace Nagule.Graphics;

using Microsoft.Extensions.Logging;
using Sia;

public class RenderFrame : ParallelFrame
{
    public World World { get; } = new World();

    protected override ILogger CreateLogger(World world, LogLibrary logLib)
        => logLib.Create<RenderFrame>();
}