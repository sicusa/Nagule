namespace Nagule.Graphics;

using Sia;

public struct RenderPipelineState : IAssetState
{
    public readonly bool Loaded => World != null;

    public EntityRef CameraEntity;
    public World World;
    public Scheduler Scheduler;
}