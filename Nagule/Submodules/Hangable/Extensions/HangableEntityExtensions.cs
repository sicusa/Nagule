namespace Nagule;

using Sia;

public static class HangableEntityExtensions
{
    public static CancellationTokenSource Hang(this EntityRef entity, Action<EntityRef> action)
    {
        var source = new CancellationTokenSource();
        Context<World>.Current!.GetAddon<HangingList>().RawEntries.Add(new(entity, action, source.Token));
        return source;
    }
}