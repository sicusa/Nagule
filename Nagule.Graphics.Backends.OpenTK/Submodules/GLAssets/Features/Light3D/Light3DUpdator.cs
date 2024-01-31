namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Sia;

public class Light3DUpdator : GraphicsUpdatorBase<EntityRef, Light3DUpdator.Entry>
{
    public record struct Entry(EntityRef StateEntity, Vector3 Position, Vector3 Direction)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => StateEntity;

        public static void Record(in EntityRef entity, ref Entry value)
        {
            var trans = entity.GetFeatureNode<Transform3D>();
            value = new(
                entity.GetStateEntity(),
                trans.WorldPosition, trans.WorldForward);
        }
    }

    [AllowNull] private Light3DLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Light3DLibrary>();
    }

    protected override void UpdateEntry(in EntityRef e, in Entry entry)
    {
        ref var state = ref e.Get<Light3DState>();
        if (state.Type == LightType.None) {
            return;
        }
        ref var pars = ref _lib.Parameters[state.Index];
        ref var buffer = ref _lib.ParametersBuffer[state.Index];
        pars.Position = buffer.Position = entry.Position;
        pars.Direction = buffer.Direction = entry.Direction;
    }
}