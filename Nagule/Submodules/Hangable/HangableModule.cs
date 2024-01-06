namespace Nagule;

using Sia;

public class HangingListExecuteSystem()
    : SystemBase(
        matcher: Matchers.Any)
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var entries = world.GetAddon<HangingList>().RawEntries;

        var count = entries.Count;
        if (count == 0) { return; }

        for (int i = 0; i < count;) {
            var (entity, action, token) = entries[i];
            if (!entity.Valid) {
                entries[i] = entries[--count];
            }
            else if (token.IsCancellationRequested) {
                action(entity);
                entries[i] = entries[--count];
            }
            else {
                i++;
            }
        }

        int removedCount = entries.Count - count;
        if (removedCount != 0) {
            entries.RemoveRange(count, removedCount);
        }
    }
}

public class HangableModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<HangingListExecuteSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<HangingList>(world);
    }
}