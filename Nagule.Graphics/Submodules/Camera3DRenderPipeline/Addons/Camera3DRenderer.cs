namespace Nagule.Graphics;

using CommunityToolkit.HighPerformance;
using Sia;

public class Camera3DRenderer : RendererBase
{
    internal List<Scheduler> Schedulers { get; } = [];

    protected override void OnRender()
    {
        foreach (var scheduler in Schedulers.AsSpan()) {
            scheduler.Tick();
        }
    }
}