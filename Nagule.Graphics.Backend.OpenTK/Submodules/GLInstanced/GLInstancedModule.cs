namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Mesh3DInstanceTransformUpdateSystem : RenderSystemBase
{
    [AllowNull] private GLMesh3DInstanceLibrary _lib;

    public Mesh3DInstanceTransformUpdateSystem(GLInstancedModule module)
    {
        Matcher = module.Mesh3DMatcher;
        Trigger = EventUnion.Of<Feature.OnTransformChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _lib = world.AcquireAddon<GLMesh3DInstanceLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<(EntityRef Entity, Matrix4x4 WorldMat)>.Allocate(count);
        query.Record(mem, static (in EntityRef entity, ref (EntityRef, Matrix4x4) value) => {
            value = (entity, entity.Get<Feature>().Node.Get<Transform3D>().World);
        });

        RenderFrame.Start(() => {
            var instancedEntries = _lib.InstanceEntries;
            foreach (ref var tuple in mem.Span) {
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

public class Mesh3DInstanceGroupSystem : RenderSystemBase
{
    private record struct EntryData(EntityRef Entity, Mesh3DInstanceGroupKey Key, Matrix4x4 WorldMatrix);

    [AllowNull] private GLMesh3DInstanceLibrary _lib;
    [AllowNull] private Mesh3DManager _meshManager;
    [AllowNull] private MaterialManager _materialManager;

    public Mesh3DInstanceGroupSystem(GLInstancedModule module)
    {
        Matcher = module.Mesh3DMatcher;
        Trigger = EventUnion.Of<
            WorldEvents.Add,
            Mesh3D.SetData,
            Mesh3D.SetMaterial>();
        Filter = EventUnion.Of<ObjectEvents.Destroy>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _lib = world.GetAddon<GLMesh3DInstanceLibrary>();
        _meshManager = world.GetAddon<Mesh3DManager>();
        _materialManager = world.GetAddon<MaterialManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<EntryData>.Allocate(count);
        query.Record(_materialManager, mem, static (in MaterialManager materialManager, in EntityRef entity, ref EntryData value) => {
            ref var mesh = ref entity.Get<Mesh3D>();
            var materialEntity = materialManager.Get(mesh.Material);
            var worldMatrix = entity.Get<Feature>().Node.Get<Transform3D>().World;
            value = new(entity, new(materialEntity, mesh.Data), worldMatrix);
        });

        RenderFrame.Start(() => {
            var groups = _lib.Groups;
            var instanceEntries = _lib.InstanceEntries;

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
                    sharedGroup = new(key, _meshManager.DataStates[key.MeshData]);
                }

                int index = sharedGroup!.Add(entity, new(mat));
                entry = (sharedGroup, index);
            }

            mem.Dispose();
            return true;
        });
    }
}

public class Mesh3DInstanceCleanSystem : RenderSystemBase
{
    [AllowNull] private GLMesh3DInstanceLibrary _lib;

    public Mesh3DInstanceCleanSystem(GLInstancedModule module)
    {
        Matcher = module.Mesh3DMatcher;
        Trigger = EventUnion.Of<ObjectEvents.Destroy>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _lib = world.AcquireAddon<GLMesh3DInstanceLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<EntityRef>.Allocate(count);
        query.Record(mem);

        RenderFrame.Start(() => {
            var groups = _lib.Groups;
            var instanceEntries = _lib.InstanceEntries;

            foreach (var entity in mem.Span) {
                ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(instanceEntries, entity);
                if (Unsafe.IsNullRef(ref entry)) {
                    continue;
                }
                var group = entry.Group;

                if (group.Count == 1) {
                    group.Dispose();
                    groups.Remove(group.Key);
                }
                else {
                    group.Remove(entry.Index);
                    if (entry.Index != group.Count) {
                        instanceEntries[group.Entities[entry.Index]] = (group, entry.Index);
                    }
                }
            }

            mem.Dispose();
            return true;
        });
    }
}

public class GLInstancedModule : AddonSystemBase
{
    public IEntityMatcher Mesh3DMatcher { get; }

    public GLInstancedModule()
        : this(Matchers.Of<Mesh3D, Feature>())
    {
    }

    public GLInstancedModule(IEntityMatcher mesh3DMatcher)
    {
        Mesh3DMatcher = mesh3DMatcher;

        Children = SystemChain.Empty
            .Add<Mesh3DInstanceTransformUpdateSystem>(() => new(this))
            .Add<Mesh3DInstanceGroupSystem>(() => new(this))
            .Add<Mesh3DInstanceCleanSystem>(() => new(this));
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<GLMesh3DInstanceLibrary>(world);
    }
}