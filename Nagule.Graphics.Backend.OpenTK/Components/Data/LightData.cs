namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightParameters
{
    public static readonly int MemorySize = Marshal.SizeOf<LightParameters>();

    public float Type;
    public float ShadowMapId;
    public Vector4 Color;
    public Vector3 Position;
    public float Range;
    public Vector3 Direction;
    public Vector2 ConeCutoffsOrAreaSize;
}

public struct LightData : IHashComponent
{
    public LightType Type;
    public ushort Index;

    public uint? ShadowCameraId;
    public int ShadowMapId;
}