namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Light3DTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Light3D>(),
        trigger: EventUnion.Of<WorldEvents.Add, Feature.OnTransformChanged>())
{
    private record struct Data(EntityRef StateEntity, Vector3 Position, Vector3 Direction);

    [AllowNull] private Light3DLibrary _lib;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _lib = world.GetAddon<Light3DLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<Data>.Allocate(count);
        query.Record(mem, static (in EntityRef entity, ref Data value) => {
            var nodeTrans = entity.GetFeatureNode().Get<Transform3D>();
            value = new(entity.GetStateEntity(), nodeTrans.WorldPosition, -nodeTrans.WorldForward);
        });

        RenderFramer.Start(() => {
            foreach (ref var tuple in mem.Span) {
                ref var state = ref tuple.StateEntity.Get<Light3DState>();
                if (state.Type != LightType.None) {
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

[NaAssetModule<RLight3D, Light3DState>(typeof(GraphicsAssetManager<,,>))]
internal partial class Light3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<Light3DTransformUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        AddAddon<Light3DLibrary>(world);
        base.Initialize(world, scheduler);
    }
}