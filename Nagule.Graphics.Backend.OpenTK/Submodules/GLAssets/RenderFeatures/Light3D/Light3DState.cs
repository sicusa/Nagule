namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Light3DParameters
{
    public static readonly int MemorySize = Marshal.SizeOf<Light3DParameters>();

    public float Type;
    public Vector4 Color;
    public Vector3 Position;
    public float Range;
    public Vector3 Direction;
    public float InnerConeAngle;
    public float OuterConeAngle;
}

public struct Light3DState : IAssetState
{
    public readonly bool Loaded => Type != LightType.None;

    public LightType Type;
    public int Index;
}