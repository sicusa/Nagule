namespace Nagule.Graphics;

using Sia;

public class RenderPipelineInfo : IAddon
{
    public World MainWorld { get; internal set; } = null!;
    public EntityRef CameraState { get; internal set; }
}