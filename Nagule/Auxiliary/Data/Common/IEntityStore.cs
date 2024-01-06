namespace Nagule;

using Sia;

public interface IEntityStore<TValue> : IReadOnlyEntityStore<TValue>
{
    ref TValue? this[EntityRef entity] { get; }

    void Add(in EntityRef entity, in TValue value);

    ref TValue? GetOrAddDefault(in EntityRef entity, out bool exists);

    bool Remove(in EntityRef entity);
    bool Remove(in EntityRef entity, out TValue value);
}