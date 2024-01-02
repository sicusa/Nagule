namespace Nagule.Graphics;

using Sia;

public class RenderPipelineLibrary : IAddon
{
    public record struct PipelineEntry(RenderPipelineScheduler Scheduler, SystemChain.Handle Handle);

    public IReadOnlyDictionary<EntityRef, PipelineEntry> Entries => EntriesRaw;

    internal readonly Dictionary<EntityRef, PipelineEntry> EntriesRaw = [];
}