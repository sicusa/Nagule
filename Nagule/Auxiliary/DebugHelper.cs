namespace Nagule;

public static class DebugHelper
{
    public static string Print(IContext context, Guid id)
    {
        if (context.TryGet<Name>(id, out var name)) {
            return $"{name.Value} [{id}]";
        }
        else {
            return $"[{id}]";
        }
    }
}