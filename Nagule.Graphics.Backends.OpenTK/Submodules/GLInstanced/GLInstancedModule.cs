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
        EntityRef Entity, Mesh3DInstanceGroupKey Key,
        Matrix4x4 WorldMatrix, LayerMask LayerMask);

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var lib = world.GetAddon<Mesh3DInstanceLibrary>();
        var meshManager = world.GetAddon<Mesh3DManager>();
        var mem = MemoryOwner<EntryData>.Allocate(count);

        query.Record(world, mem, static (in World world, in EntityRef entity, ref EntryData value) => {
            ref var mesh = ref entity.Get<Mesh3D>();
            ref var feature = ref entity.Get<Feature>();
            var nodeEntity = feature.Node;
            var matEntity = world.GetAsset(mesh.Material);

            value.Entity = entity;
            value.Key = new(matEntity.GetStateEntity(), mesh.Data);

            value.WorldMatrix =
                feature.IsEnabled
                    ? nodeEntity.Get<Transform3D>().World : default;

            value.LayerMask = nodeEntity.Get<Node3D>().Layer;
            if (mesh.IsShadowCaster) {
                value.LayerMask |= GLInternalLayers.ShadowCaster.Mask;
            }
        });

        RenderFramer.Start(() => {
            var groups = lib.Groups;
            var instanceEntries = lib.InstanceEntries;

            foreach (ref var data in mem.Span) {
                var entity = data.Entity;
                var key = data.Key;

                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    instanceEntries, entity, out bool exists);
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
                        instanceEntries[group.Entities[entry.Index]] = (group, entry.Index);
                    }
                }

                ref var sharedGroup = ref CollectionsMarshal.GetValueRefOrAddDefault(groups, key, out exists);
                if (!exists) {
                    sharedGroup = new(key, meshManager.DataBuffers[key.MeshData]);
                }

                int index = sharedGroup!.Add(entity);
                entry = (sharedGroup, index);

                ref var instance = ref sharedGroup.InstanceBuffer[index];
                instance.ObjectToWorld = data.WorldMatrix;
                instance.LayerMask = data.LayerMask;
            }

            mem.Dispose();
            return true;
        });
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