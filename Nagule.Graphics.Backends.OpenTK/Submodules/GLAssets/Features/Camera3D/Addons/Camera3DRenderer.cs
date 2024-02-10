namespace Nagule.Graphics;

using CommunityToolkit.HighPerformance;
using Sia;

public class Camera3DRenderer : RenderAddonBase
{
    private record struct Entry(RenderPriority Priority, EntityRef PipelineStateEntity);
    private readonly List<Entry> _entries = [];

    public void Register(RenderPriority priority, EntityRef pipelineStateEntity)
    {
        var index = _entries.FindIndex(e => e.Priority >= priority);
        if (index == -1) {
            index = _entries.Count;
        }
        _entries.Insert(index, new(priority, pipelineStateEntity));
    }

    public bool Unregister(EntityRef pipelineStateEntity)
    {
        int index = _entries.FindIndex(
            e => e.PipelineStateEntity == pipelineStateEntity);
        if (index == -1) { return false; }
        _entries.RemoveAt(index);
        return true;
    }

    protected override void OnRender()
    {
        foreach (var (_, pipelineState) in _entries.AsSpan()) {
            pipelineState.Get<RenderPipelineState>().Scheduler.Tick();
        }
    }
}