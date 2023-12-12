namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Light3DTransformUpdateSystem : RenderSystemBase
{
    private record struct Data(EntityRef Entity, Vector3 Position, Vector3 Direction);

    [AllowNull] private Light3DLibrary _lib;
    [AllowNull] private Light3DManager _manager;

    public Light3DTransformUpdateSystem()
    {
        Matcher = Matchers.Of<Light3D>();
        Trigger = EventUnion.Of<WorldEvents.Add, Feature.OnTransformChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _lib = world.GetAddon<Light3DLibrary>();
        _manager = world.GetAddon<Light3DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<Data>.Allocate(count);
        query.Record(mem, static (in EntityRef entity, ref Data value) => {
            var nodeTrans = entity.Get<Feature>().Node.Get<Transform3D>();
            value = new(entity, nodeTrans.WorldPosition, nodeTrans.WorldForward);
        });

        RenderFrame.Start(() => {
            foreach (ref var tuple in mem.Span) {
                ref var state = ref _manager.RenderStates.GetOrNullRef(tuple.Entity);
                if (!Unsafe.IsNullRef(ref state)) {
                    ref var pars = ref _lib.Parameters[state.Index];
                    ref var buffer = ref _lib.GetBufferData(state.Index);
                    pars.Position = buffer.Position = tuple.Position;
                    pars.Direction = buffer.Direction = tuple.Direction;
                }
            }
            mem.Dispose();
            return true;
        });
    }
}

public class Light3DModule : AddonSystemBase
{
    public Light3DModule()
    {
        Children = SystemChain.Empty
            .Add<Light3DTransformUpdateSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Light3DLibrary>(world);
        AddAddon<Light3DManager>(world);
    }
}