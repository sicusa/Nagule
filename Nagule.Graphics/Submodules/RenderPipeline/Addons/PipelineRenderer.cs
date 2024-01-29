namespace Nagule.Graphics;

using CommunityToolkit.HighPerformance;
using Sia;

public class PipelineRenderer : RenderAddonBase
{
    internal record struct Entry(RenderPriority Priority, Scheduler Scheduler);
    internal List<Entry> Entries { get; } = [];

    protected override void OnRender()
    {
        foreach (var (_, scheduler) in Entries.AsSpan()) {
            scheduler.Tick();
        }
    }
}