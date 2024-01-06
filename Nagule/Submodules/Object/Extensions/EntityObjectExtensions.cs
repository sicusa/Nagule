namespace Nagule;

using Sia;

public static class EntityObjectExtensions
{
    public static void Destroy(this EntityRef entity)
        => entity.Send(ObjectEvents.Destroy.Instance);

    public static void CancelDestroy(this EntityRef entity)
        => entity.Send(HOEvents.Cancel<ObjectEvents.Destroy>.Instance);
}