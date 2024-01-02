namespace Nagule;

using Sia;

public class PrimaryWindow : ViewBase<TypeUnion<Window>>
{
    public EntityRef Entity => _entity ?? throw new NullReferenceException("Primary window not set");
    public bool HasValue => _entity.HasValue;

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