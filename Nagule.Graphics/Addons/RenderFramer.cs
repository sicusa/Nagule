namespace Nagule.Graphics;

using Microsoft.Extensions.Logging;
using Sia;

public class RenderFramer : ParallelFramer
{
    protected override ILogger CreateLogger(World world, LogLibrary logLib)
        => logLib.Create<RenderFramer>();
}