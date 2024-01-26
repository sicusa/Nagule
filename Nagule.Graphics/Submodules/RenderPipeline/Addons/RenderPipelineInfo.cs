namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderPipelineInfo : IAddon
{
    [AllowNull] public World MainWorld { get; internal set; }
    public EntityRef CameraState { get; internal set; }
}