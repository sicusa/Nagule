namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Mesh3DInstanceTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Mesh3D>(),
        trigger: EventUnion.Of<
            Feature.OnIsEnabledChanged,
            Feature.OnNodeTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => world.GetAddon<Mesh3DInstanceUpdator>().Record(query);
}

public class Mesh3DInstanceGroupSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Mesh3D>(),
        trigger: EventUnion.Of<
            WorldEvents.Add,
            Mesh3D.SetData,
            Mesh3D.SetMaterial>())
{
    private record struct EntryData(
        AssetId AssetId, Mesh3DInstanceGroupKey Key,
        Matrix4x4 WorldMatrix, LayerMask LayerMask);
    
    private Mesh3DInstanceLibrary _lib = null!;
    private Mesh3DManager _manager = null!;
    
    private MemoryOwner<EntryData>? _mem;
    private int _acc;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _lib = world.GetAddon<Mesh3DInstanceLibrary>();
        _manager = world.GetAddon<Mesh3DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        _mem = MemoryOwner<EntryData>.Allocate(count);
        _acc = -1;

        query.ForSliceOnParallel<AssetMetadata, Mesh3D, Feature>(OnExecute);
        RenderFramer.Start(_mem, (framer, mem) => OnRenderFramer(mem));
    }

    private void OnExecute(ref AssetMetadata meta, ref Mesh3D mesh, ref Feature feature)
    {
        var matEntity = World.GetAsset(mesh.Material);
        var nodeEntity = feature.Node;

        ref var entry = ref _mem!.Span[Interlocked.Increment(ref _acc)];

        entry.AssetId = meta.AssetId;
        entry.Key = new(matEntity.GetStateEntity(), mesh.Data);

        entry.WorldMatrix =
            feature.IsEnabled
                ? nodeEntity.Get<Transform3D>().WorldMatrix : default;

        entry.LayerMask = nodeEntity.Get<Node3D>().Layer;
        if (mesh.IsShadowCaster) {
            entry.LayerMask |= GLInternalLayers.ShadowCaster.Mask;
        }
    }

    private bool OnRenderFramer(MemoryOwner<EntryData> mem)
    {
        var groups = _lib.Groups;
        var instanceEntries = _lib.InstanceEntries;

        foreach (ref var data in mem.Span) {
            var key = data.Key;

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                instanceEntries, data.AssetId, out bool exists);
            var group = entry.Group;

            if (exists) {
                if (entry!.Group.Key == key) {
                    continue;
                }
                if (group.Count == 1) {
                    group.Dispose();
                    groups.Remove(group.Key);
                }
                else {
                    group.Remove(entry.Index);
                    instanceEntries[group.AssetIds[entry.Index]] = (group, entry.Index);
                }
            }

            ref var sharedGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(groups, key, out exists);
            if (!exists) {
                sharedGroup = new(key, _manager.DataBuffers[key.MeshData]);
            }

            int index = sharedGroup!.Add(data.AssetId);
            entry = (sharedGroup, index);

            ref var instance = ref sharedGroup.InstanceBuffer[index];
            instance.ObjectToWorld = data.WorldMatrix;
            instance.LayerMask = data.LayerMask;
        }

        mem.Dispose();
        return true;
    }
}

public class GLInstancedModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<Mesh3DInstanceTransformUpdateSystem>()
            .Add<Mesh3DInstanceGroupSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Mesh3DInstanceLibrary>(world);
        AddAddon<Mesh3DInstanceCleaner>(world);
        AddAddon<Mesh3DInstanceUpdator>(world);
    }
}