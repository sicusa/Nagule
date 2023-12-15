namespace Nagule;

using Sia;

public static class WorldObjectExtensions
{
    public static void Destroy(this World world, in EntityRef entity)
        => world.Send(entity, ObjectEvents.Destroy.Instance);

    public static void CancelDestroy(this World world, in EntityRef entity)
        => world.Send(entity, HOEvents.Cancel<ObjectEvents.Destroy>.Instance);
}