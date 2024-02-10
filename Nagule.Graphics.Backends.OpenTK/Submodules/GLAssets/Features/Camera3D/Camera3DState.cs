namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using Sia;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Camera3DParameters
{
    public static readonly int MemorySize = Marshal.SizeOf<Camera3DParameters>();

    public Matrix4x4 View;
    public Matrix4x4 Proj;
    public Matrix4x4 ProjInv;
    public Matrix4x4 ViewProj;
    public Vector3 Position;
    public float NearPlaneDistance;
    public float FarPlaneDistance;
}

public record struct Camera3DState : IAssetState
{
    public readonly bool Loaded => Handle != BufferHandle.Zero;

    public BufferHandle Handle;
    public IntPtr Pointer;
    public Camera3DParameters Parameters;
    public long ParametersVersion;

    public ClearFlags ClearFlags;
    public Matrix4x4 Projection;

    public EntityRef SettingsState;
    public IRenderTarget? RenderTarget;
}