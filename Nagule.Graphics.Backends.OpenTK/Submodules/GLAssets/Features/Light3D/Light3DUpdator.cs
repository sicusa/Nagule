namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Sia;

public class Light3DUpdator : GraphicsUpdatorBase<EntityRef, Light3DUpdator.Entry>
{
    public record struct Entry(EntityRef StateEntity, bool IsEnabled, Vector3 Position, Vector3 Direction)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => StateEntity;

        public static bool Record(in EntityRef entity, ref Entry value)
        {
            ref var feature = ref entity.Get<Feature>();
            if (feature.IsEnabled) {
                ref var trans = ref feature.Node.Get<Transform3D>();
                value = new(
                    entity.GetStateEntity(), true,
                    trans.WorldPosition, trans.WorldForward);
            }
            else {
                value = new(entity.GetStateEntity(), false, default, default);
            }

            return true;
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
        state.IsEnabled = entry.IsEnabled;
        if (!entry.IsEnabled) {
            return;
        }
        if (state.Type == LightType.None) {
            return;
        }
        ref var pars = ref _lib.Parameters[state.Index];
        ref var buffer = ref _lib.ParametersBuffer[state.Index];
        pars.Position = buffer.Position = entry.Position;
        pars.Direction = buffer.Direction = entry.Direction;
    }
}