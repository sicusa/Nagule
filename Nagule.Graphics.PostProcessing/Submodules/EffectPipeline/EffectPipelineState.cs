namespace Nagule.Graphics.PostProcessing;

using Sia;

public record struct EffectPipelineState() : IAssetState
{
    public readonly bool Loaded => MaterialEntity != default;

    public readonly IReadOnlyDictionary<string, EntityRef> Effects => EffectsRaw;
    public readonly IReadOnlyList<string> EffectSequence => EffectSequenceRaw;

    public EntityRef MaterialEntity { get; internal set; }

    internal readonly Dictionary<string, EntityRef> EffectsRaw = [];
    internal readonly List<string> EffectSequenceRaw = [];
}