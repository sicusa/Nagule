namespace Nagule.Graphics.Backends.OpenTK;

using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public interface IGraphicsUpdatorEntry<TKey, TEntry>
{
    TKey Key { get; }
    static abstract void Record(in EntityRef entity, ref TEntry value);
}

public abstract class GraphicsUpdatorBase<TKey, TEntry> : RenderAddonBase
    where TKey : notnull
    where TEntry : struct, IGraphicsUpdatorEntry<TKey, TEntry>
{
    private readonly Dictionary<TKey, (MemoryOwner<TEntry>, int)> _pendingDict = [];

    public void Record(IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<TEntry>.Allocate(count);
        query.Record(mem, TEntry.Record);

        RenderFramer.Start(() => {
            int i = 0;
            foreach (ref var entry in mem.Span) {
                _pendingDict[entry.Key] = (mem, i);
                i++;
            }
            return true;
        });
    }

    protected sealed override void OnRender()
    {
        foreach (var (e, (mem, index)) in _pendingDict) {
            UpdateEntry(e, mem.Span[index]);
        }
        _pendingDict.Clear();
    }

    protected abstract void UpdateEntry(in TKey key, in TEntry entry);
}