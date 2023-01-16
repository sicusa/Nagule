namespace Nagule;

public struct Children : IPooledComponent
{
    public IReadOnlyList<Guid> Ids => IdsRaw;

    internal readonly List<Guid> IdsRaw = new();

    public Children() {}

    public static void Recurse(IContext context, Guid id, Action<IContext, Guid> action)
    {
        action(context, id);
        RecurseChildren(context, id, action);
    }

    public static void RecurseChildren(IContext context, Guid id, Action<IContext, Guid> action)
    {
        if (!context.TryGet<Children>(id, out var children)) {
            return;
        }
        var childrenIds = children.Ids;
        for (int i = 0; i != childrenIds.Count; ++i) {
            var childId = childrenIds[i];
            action(context, childId);
            RecurseChildren(context, childId, action);
        }
    }

    public static void Recurse<T>(
        IContext context, Guid id, T initial, Func<IContext, Guid, T, T> transformer)
        => RecurseChildren(context, id, transformer(context, id, initial), transformer);

    public static void RecurseChildren<T>(
        IContext context, Guid id, T initial, Func<IContext, Guid, T, T> transformer)
    {
        if (!context.TryGet<Children>(id, out var children)) {
            return;
        }
        var childrenIds = children.Ids;
        for (int i = 0; i != childrenIds.Count; ++i) {
            var childId = childrenIds[i];
            var res = transformer(context, childId, initial);
            RecurseChildren(context, childId, res, transformer);
        }
    }

    public static void RecurseTerminabe<T>(
        IContext context, Guid id, T initial, Func<IContext, Guid, T, (bool, T)> transformer)
    {
        var (cont, res) = transformer(context, id, initial);
        if (cont) {
            RecurseChildrenTerminable(context, id, res, transformer);
        }
    }

    public static void RecurseChildrenTerminable<T>(
        IContext context, Guid id, T initial, Func<IContext, Guid, T, (bool, T)> transformer)
    {
        if (!context.TryGet<Children>(id, out var children)) {
            return;
        }
        var childrenIds = children.Ids;
        for (int i = 0; i != childrenIds.Count; ++i) {
            var childId = childrenIds[i];
            var (cont, res) = transformer(context, childId, initial);
            if (cont) {
                RecurseChildrenTerminable(context, childId, res, transformer);
            }
        }
    }
}