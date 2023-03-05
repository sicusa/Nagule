namespace Nagule;

using System.Diagnostics.CodeAnalysis;

using Aeco;
using Aeco.Reactive;

public interface ISingletonComponent : IComponent
{
    static virtual Guid Id { get; } = Guid.Empty;
}

public static class SingletonComponentExtensions
{
    public static bool Contains<TComponent>(
        this IBasicDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => dataLayer.Contains<TComponent>(TComponent.Id);

    public static ref readonly TComponent Inspect<TComponent>(
        this IReadableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => ref dataLayer.Inspect<TComponent>(TComponent.Id);

    public static ref readonly TComponent InspectOrNullRef<TComponent>(
        this IReadableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => ref dataLayer.InspectOrNullRef<TComponent>(TComponent.Id);

    public static bool TryGet<TComponent>(
        this IReadableDataLayer<IComponent> dataLayer, [MaybeNullWhen(false)] out TComponent component)
        where TComponent : ISingletonComponent
        => dataLayer.TryGet<TComponent>(TComponent.Id, out component);
    
    public static ref TComponent InspectRaw<TComponent>(
        this IWritableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => ref dataLayer.InspectRaw<TComponent>(TComponent.Id);

    public static ref TComponent Require<TComponent>(
        this IWritableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => ref dataLayer.Require<TComponent>(TComponent.Id);

    public static ref TComponent RequireOrNullRef<TComponent>(
        this IWritableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => ref dataLayer.RequireOrNullRef<TComponent>(TComponent.Id);

    public static ref TComponent Acquire<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent, new()
        => ref dataLayer.Acquire<TComponent>(TComponent.Id);

    public static ref TComponent Acquire<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer, out bool exists)
        where TComponent : ISingletonComponent, new()
        => ref dataLayer.Acquire<TComponent>(TComponent.Id, out exists);

    public static ref TComponent AcquireRaw<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent, new()
        => ref dataLayer.AcquireRaw<TComponent>(TComponent.Id);
    
    public static bool Remove<TComponent>(
        this IShrinkableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => dataLayer.Remove<TComponent>(TComponent.Id);

    public static bool Remove<TComponent>(
        this IShrinkableDataLayer<IComponent> dataLayer, [MaybeNullWhen(false)] out TComponent component)
        where TComponent : ISingletonComponent
        => dataLayer.Remove<TComponent>(TComponent.Id, out component);

    public static ref TComponent AcquireRaw<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer, out bool exists)
        where TComponent : ISingletonComponent, new()
        => ref dataLayer.AcquireRaw<TComponent>(TComponent.Id, out exists);
    
    public static ref TComponent Set<TComponent>(
        this ISettableDataLayer<IComponent> dataLayer, in TComponent component)
        where TComponent : ISingletonComponent, new()
        => ref dataLayer.Set<TComponent>(TComponent.Id, component);

    public static void MarkCreated<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => dataLayer.MarkCreated<TComponent>(TComponent.Id);

    public static void MarkModified<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => dataLayer.MarkModified<TComponent>(TComponent.Id);

    public static void MarkRemoved<TComponent>(
        this IExpandableDataLayer<IComponent> dataLayer)
        where TComponent : ISingletonComponent
        => dataLayer.MarkRemoved<TComponent>(TComponent.Id);
}