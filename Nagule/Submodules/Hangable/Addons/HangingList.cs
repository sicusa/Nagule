namespace Nagule;

using Sia;

public class HangingList : IAddon
{
    public record struct Entry(EntityRef Entity, Action<EntityRef> Action, CancellationToken Token);

    public IReadOnlyList<Entry> Entries => RawEntries;

    internal readonly List<Entry> RawEntries = [];
}