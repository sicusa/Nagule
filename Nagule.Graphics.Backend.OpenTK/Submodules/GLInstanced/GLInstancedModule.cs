namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Mesh3DInstanceTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Mesh3D>(),
        trigger: EventUnion.Of<Feature.OnTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int queryCount = query.Count;
        if (queryCount == 0) { return; }

        var mem = MemoryOwner<(EntityRef Entity, Matrix4x4 WorldMat)>.Allocate(queryCount);
        int count = query.Record(mem, static (in EntityRef entity, ref (EntityRef, Matrix4x4) value) => {
            var node = entity.GetFeatureNode();
            value = (entity, node.Get<Transform3D>().World);
            return true;
        });

        var lib = world.GetAddon<GLMesh3DInstanceLibrary>();

        RenderFrame.Start(() => {
            var instancedEntries = lib.InstanceEntries;
            foreach (ref var tuple in mem.Span[0..count]) {
                ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(instancedEntries, tuple.Entity);
                if (!Unsafe.IsNullRef(ref entry)) {
                    entry.Group[entry.Index] = new(tuple.WorldMat);
                }
            }
            mem.Dispose();
            return true;
        });
    }
}

public class Mesh3DInstanceGroupSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Mesh3D>(),
        trigger: EventUnion.Of<
            WorldEvents.Add,
            Mesh3D.SetData,
            Mesh3D.SetMaterial>())
{
    private record struct EntryData(EntityRef Entity, Mesh3DInstanceGroupKey Key, Matrix4x4 WorldMatrix);

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var lib = world.GetAddon<GLMesh3DInstanceLibrary>();
        var meshManager = world.GetAddon<Mesh3DManager>();
        var materialManager = world.GetAddon<MaterialManager>();

        var mem = MemoryOwner<EntryData>.Allocate(count);
        query.Record(materialManager, mem, static (in MaterialManager materialManager, in EntityRef entity, ref EntryData value) => {
            ref var mesh = ref entity.Get<Mesh3D>();
            var materialEntity = materialManager[mesh.Material];
            var worldMatrix = entity.GetFeatureNode().Get<Transform3D>().World;
            value = new(entity, new(materialEntity, mesh.Data), worldMatrix);
        });

        RenderFrame.Start(() => {
            var groups = lib.Groups;
            var instanceEntries = lib.InstanceEntries;

            foreach (var (entity, key, mat) in mem.Span) {
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

                int index = sharedGroup!.Add(entity, new(mat));
                entry = (sharedGroup, index);
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
        AddAddon<GLMesh3DInstanceLibrary>(world);
        AddAddon<GLMesh3DInstanceCleaner>(world);
    }
}