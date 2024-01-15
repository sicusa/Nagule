namespace Nagule.Graphics.PostProcessing;

using System.Numerics;
using Sia;

[SiaTemplate(nameof(ProcedualSkybox))]
[NaAsset]
public record RProcedualSkybox : REffectBase
{
    public int LightSamples { get; init; } = 8;
    public int Samples { get; init; } = 16;

    public Vector3 Ground { get; init; } = Vector3.Zero;

    public int StarsLayers { get; init; } = 0;
    public float StarsElevation { get; init; } = 0.01f;
    public float StarsAzimuth { get; init; } = 0.05f;
}