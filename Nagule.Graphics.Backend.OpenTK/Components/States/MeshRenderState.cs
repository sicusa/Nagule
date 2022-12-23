namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

[StructLayout(LayoutKind.Sequential)]
public struct MeshInstance
{
    public const int MemorySize = 64;
    public Matrix4x4 ObjectToWorld;
}

public struct MeshRenderState : IPooledComponent
{
    public const int InitialCapacity = 64;

    public int InstanceCount = 0;

    [AllowNull] public MeshInstance[] Instances = null;
    [AllowNull] public Guid[] InstanceIds = null;

    public int MinimumEmptyIndex = 0;
    public int MaximumEmptyIndex = 0;
    public int MaximumInstanceIndex = 0;

    public readonly List<Guid> VariantIds = new();

    public MeshRenderState() {}
}