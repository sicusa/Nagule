namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using Sia;

public class Light3DUpdator : GLBufferUpdatorBase<AssetId, Light3DUpdator.Entry>
{
    public record struct Entry(
        AssetId AssetId, EntityRef StateEntity, bool IsEnabled, Vector3 Position, Vector3 Direction)
        : IGraphicsUpdatorEntry<AssetId, Entry>
    {
        public readonly AssetId Key => AssetId;

        public static void Record(in EntityRef entity, out Entry value)
        {
            var id = entity.GetAssetId();
            var stateEntity = entity.GetStateEntity();
            ref var feature = ref entity.Get<Feature>();

            if (feature.IsEnabled) {
                ref var trans = ref feature.Node.Get<Transform3D>();
                value = new(id, stateEntity, true,
                    trans.WorldPosition, trans.WorldForward);
            }
            else {
                value = new(id, stateEntity, false, default, default);
            }
        }
    }

    private Light3DLibrary _lib = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Light3DLibrary>();
    }

    protected override void UpdateEntry(AssetId id, in Entry entry)
    {
        ref var state = ref entry.StateEntity.Get<Light3DState>();
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