namespace Nagule;

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class EntityStore<TValue> : IReadOnlyEntityStore<TValue>
{
    public int Count => _dict.Count;

    public IEnumerable<EntityRef> Entities => _dict.Keys;
    public IEnumerable<TValue> Values => _dict.Values;

    private readonly Dictionary<EntityRef, TValue> _dict = [];

    public ref TValue GetOrNullRef(in EntityRef entity)
        => ref CollectionsMarshal.GetValueRefOrNullRef(_dict, entity);

    public ref TValue Get(in EntityRef entity)
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(_dict, entity);
        if (Unsafe.IsNullRef(ref value)) {
            throw new KeyNotFoundException("Value not found in entity store");
        }
        return ref value!;
    }

    public ref TValue Set(in EntityRef entity, in TValue value)
    {
        ref var valueRef = ref Acquire(entity);
        valueRef = value;
        return ref valueRef!;
    }

    public bool Remove(in EntityRef entity)
        => _dict.Remove(entity);

    public bool Remove(in EntityRef entity, out TValue value)
        => _dict.Remove(entity, out value!);

    public ref TValue? Acquire(in EntityRef entity)
        => ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, entity, out bool _);

    public ref TValue? Acquire(in EntityRef entity, out bool exists)
        => ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, entity, out exists);

    public IEnumerator<KeyValuePair<EntityRef, TValue>> GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}