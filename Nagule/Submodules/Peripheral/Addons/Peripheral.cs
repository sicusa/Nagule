namespace Nagule;

using Sia;

public class Peripheral : ViewBase<TypeUnion<Keyboard, Mouse, Cursor>>
{
    public ref Keyboard Keyboard => ref Entity.Get<Keyboard>();
    public ref Mouse Mouse => ref Entity.Get<Mouse>();
    public ref Cursor Cursor => ref Entity.Get<Cursor>();

    public EntityRef Entity { get; set; }

    protected override void OnEntityAdded(in EntityRef entity)
    {
        Entity = entity;
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        if (Entity == entity) {
            Entity = default;
        }
    }
}