namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

[StructLayout(LayoutKind.Sequential)]
public struct MeshInstance
{
    public static readonly int MemorySize = Unsafe.SizeOf<MeshInstance>();
    public Matrix4x4 ObjectToWorld;
}

public struct MeshRenderState : IHashComponent
{
    public const int InitialCapacity = 64;

    public int InstanceCount = 0;
    [AllowNull] public MeshInstance[] Instances = null;
    [AllowNull] public uint[] InstanceIds = null;

    public MeshRenderState() {}
}