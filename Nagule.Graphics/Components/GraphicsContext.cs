namespace Nagule.Graphics;

using System.Numerics;
using Sia;

public partial record struct GraphicsContext()
{
    [SiaProperty] public double? RenderFrequency;
    [SiaProperty] public VSyncMode VSyncMode = VSyncMode.Off;
    [SiaProperty] public Vector4 ClearColor = Vector4.Zero;
    [SiaProperty] public bool IsDebugEnabled = false;
}