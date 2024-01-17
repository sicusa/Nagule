namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public record struct EffectEnvironmentState : IAssetState
{
    public readonly bool Loaded => PipelineEntity.Valid;
    public EntityRef PipelineEntity;
}