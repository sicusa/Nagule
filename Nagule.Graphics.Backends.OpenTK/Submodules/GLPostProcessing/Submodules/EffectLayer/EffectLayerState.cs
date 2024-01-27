namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public record struct EffectLayerState : IAssetState
{
    public readonly bool Loaded => PipelineEntity.Valid;
    public EntityRef PipelineEntity;
}