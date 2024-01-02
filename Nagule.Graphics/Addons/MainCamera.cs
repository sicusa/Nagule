namespace Nagule.Graphics;

using Sia;

public class MainCamera3D : ViewBase<TypeUnion<Camera3D>>
{
    public EntityRef Entity => _entity ?? throw new NullReferenceException("Main camera not set");
    public bool HasEntity => _entity.HasValue;

    private EntityRef? _entity;

    protected override void OnEntityAdded(in EntityRef entity)
    {
        _entity ??= entity;
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        if (_entity == entity) {
            _entity = null;
        }
    }
}