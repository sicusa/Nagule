namespace Nagule.Graphics;

using Sia;

public class MainCamera3D : ViewBase<TypeUnion<Camera3D>>
{
    public EntityRef? Entity { get; private set; }

    protected override void OnEntityAdded(in EntityRef entity)
    {
        Entity ??= entity;
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        if (Entity == entity) {
            Entity = null;
        }
    }
}