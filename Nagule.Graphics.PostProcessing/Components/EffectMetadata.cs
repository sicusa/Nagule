namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

public partial record struct EffectMetadata()
{
    [SiaProperty(Item = "Property")]
    public ImmutableDictionary<string, Dyn> Properties { get; private set; }
        = ImmutableDictionary<string, Dyn>.Empty;

    public EntityRef? PipelineEntity { get; internal set; }
    public string Source { get; internal set; }
    public string EntryPoint { get; internal set; }
}