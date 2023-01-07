namespace Nagule.Graphics;

using System.Runtime.CompilerServices;

using Aeco;

public struct GraphicsResourceValid : IPooledComponent
{
}

public static class GraphicsResourceValidExtensions
{
    public static ref readonly TComponent InspectValidGraphics<TComponent>(this IContext context, Guid id, out bool valid)
        where TComponent : IComponent
    {
        if (context.Contains<GraphicsResourceValid>(id)) {
            valid = true;
            return ref context.Inspect<TComponent>(id);
        }
        else {
            valid = false;
            return ref Unsafe.NullRef<TComponent>();
        }
    }
    
    public static ref TComponent RequireValidGraphics<TComponent>(this IContext context, Guid id, out bool valid)
        where TComponent : IComponent
    {
        if (context.Contains<GraphicsResourceValid>(id)) {
            valid = true;
            return ref context.Require<TComponent>(id);
        }
        else {
            valid = false;
            return ref Unsafe.NullRef<TComponent>();
        }
    }
}