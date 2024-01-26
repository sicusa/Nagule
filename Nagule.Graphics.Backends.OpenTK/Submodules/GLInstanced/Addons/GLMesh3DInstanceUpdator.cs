namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class GLMesh3DInstanceUpdator : RendererBase
{
    public record struct Entry(EntityRef Entity, Matrix4x4 WorldMat);

    internal Dictionary<EntityRef, (MemoryOwner<Entry> Memory, int Index)> PendingDict { get; } = [];

    [AllowNull] private GLMesh3DInstanceLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<GLMesh3DInstanceLibrary>();
    }

    protected override void OnRender()
    {
        var instancedEntries = _lib.InstanceEntries;
        foreach (var (e, (mem, index)) in PendingDict) {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(instancedEntries, e);
            if (!Unsafe.IsNullRef(ref entry)) {
                entry.Group[entry.Index] = mem.Span[index].WorldMat;
            }
        }
        PendingDict.Clear();
    }
}