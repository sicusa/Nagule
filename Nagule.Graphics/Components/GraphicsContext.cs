namespace Nagule.Graphics;

using System.Numerics;
using Sia;

public partial record struct GraphicsContext()
{
    [Sia] public double? RenderFrequency;
    [Sia] public VSyncMode VSyncMode = VSyncMode.Off;
    [Sia] public Vector4 ClearColor = Vector4.Zero;
    [Sia] public bool IsDebugEnabled = false;
}