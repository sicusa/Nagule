namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

public record struct EffectMetadata()
{
    public ImmutableDictionary<string, Dyn> Properties { get; set; }
        = ImmutableDictionary<string, Dyn>.Empty;

    public EntityRef? PipelineEntity { get; internal set; }
    public string Source { get; internal set; }
    public string EntryPoint { get; internal set; }
}