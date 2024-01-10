namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class PipelineInfo : IAddon
{
    public EntityRef CameraState { get; internal set; }
    [AllowNull] public World MainWorld { get; internal set; }
}