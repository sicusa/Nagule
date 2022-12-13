namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;

public enum LightCategory
{
    None = 0,
    Ambient,
    Directional,
    Point,
    Spot,
    Area
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightParameters
{
    public static readonly int MemorySize = Marshal.SizeOf<LightParameters>();

    public float Category;

    public Vector4 Color;
    public Vector3 Position;

    public float AttenuationConstant;
    public float AttenuationLinear;
    public float AttenuationQuadratic;

    public Vector3 Direction;
    public Vector2 ConeCutoffsOrAreaSize;
}

public struct LightData : IPooledComponent
{
    public int Index = -1;
    public LightCategory Category = LightCategory.None;
    public float Range = float.PositiveInfinity;

    public LightData() {}
}