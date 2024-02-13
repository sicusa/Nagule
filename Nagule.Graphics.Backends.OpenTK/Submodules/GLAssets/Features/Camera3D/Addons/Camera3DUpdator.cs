namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using Sia;

public class Camera3DUpdator : GLBufferUpdatorBase<EntityRef, Camera3DUpdator.Entry>
{
    public record struct Entry(
        EntityRef StateEntity, Camera3D Camera, Matrix4x4 View, Vector3 Position, Vector3 Direction)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => StateEntity;

        public static bool Record(in EntityRef entity, ref Entry value)
        {
            ref var trans = ref entity.GetFeatureNode<Transform3D>();
            value = new(
                entity.GetStateEntity(),
                entity.Get<Camera3D>(),
                trans.View, trans.WorldPosition, trans.WorldForward);
            return true;
        }
    }

    private Camera3DManager _manager = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _manager = world.GetAddon<Camera3DManager>();
    }

    protected override void UpdateEntry(in EntityRef e, in Entry entry)
    {
        ref var state = ref e.Get<Camera3DState>();
        if (!state.Loaded) {
            return;
        }
        _manager.UpdateCameraTransform(ref state, entry.View, entry.Position);
    }
}