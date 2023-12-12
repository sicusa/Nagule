namespace Nagule;

using Sia;

public interface IReadOnlyEntityStore<TValue> : IReadOnlyCollection<KeyValuePair<EntityRef, TValue>>
{
    public IEnumerable<EntityRef> Entities { get; }
    public IEnumerable<TValue> Values { get; }

    ref TValue Get(in EntityRef entity);
    ref TValue GetOrNullRef(in EntityRef entity);
}