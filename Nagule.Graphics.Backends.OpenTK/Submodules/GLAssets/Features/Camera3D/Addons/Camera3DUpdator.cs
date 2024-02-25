namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using Sia;

public class Camera3DUpdator : GLBufferUpdatorBase<AssetId, Camera3DUpdator.Entry>
{
    public record struct Entry(
        AssetId AssetId, EntityRef StateEntity, Camera3D Camera,
        Matrix4x4 View, Vector3 Position, Vector3 Direction)
        : IGraphicsUpdatorEntry<AssetId, Entry>
    {
        public readonly AssetId Key => AssetId;

        public static void Record(in EntityRef entity, out Entry value)
        {
            ref var trans = ref entity.GetFeatureNode<Transform3D>();
            value = new Entry {
                AssetId = entity.GetAssetId(),
                StateEntity = entity.GetStateEntity(),
                Camera = entity.Get<Camera3D>(),
                View = trans.ViewMatrix,
                Position = trans.WorldPosition,
                Direction = trans.WorldForward
            };
        }
    }

    private Camera3DManager _manager = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _manager = world.GetAddon<Camera3DManager>();
    }

    protected override void UpdateEntry(AssetId id, in Entry entry)
    {
        ref var state = ref entry.StateEntity.Get<Camera3DState>();
        if (!state.Loaded) {
            return;
        }
        _manager.UpdateCameraTransform(ref state, entry.View, entry.Position);
    }
}