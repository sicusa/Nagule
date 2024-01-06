namespace Nagule;

using System.Collections;
using System.Diagnostics.CodeAnalysis;

public class EnumDictionary<TKey, TValue>
    : IReadOnlyDictionary<TKey, TValue>, IStructuralComparable, IStructuralEquatable, ICloneable
    where TKey : struct, Enum
{
    public IEnumerable<TKey> Keys => s_keys;
    public IEnumerable<TValue> Values {
        get {
            foreach (var key in s_keys) {
                yield return this[key];
            }
        }
    }

    public int Count => _array.Length;
    public bool IsReadOnly => ((ICollection<TValue>)_array).IsReadOnly;

    public TValue[] Raw => _array;

    private readonly TValue[] _array;

    private static readonly int s_lower;
    private static readonly int s_upper;
    private static readonly TKey[] s_keys = Enum.GetValues<TKey>();

    public ref TValue this[TKey key]
        => ref _array[Convert.ToInt32(key) - s_lower];
    
    TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
        => _array[Convert.ToInt32(key) - s_lower];

    static EnumDictionary()
    {
        var values = Enum.GetValues(typeof(TKey)).Cast<TKey>();
        s_lower = Convert.ToInt32(values.Min());
        s_upper = Convert.ToInt32(values.Max());
    }

    public EnumDictionary()
    {
        _array = new TValue[1 + s_upper - s_lower];
    }

    public EnumDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        : this()
    {
        foreach (var pair in pairs) {
            this[pair.Key] = pair.Value;
        }
    }

    private EnumDictionary(EnumDictionary<TKey, TValue> other)
    {
        _array = (TValue[])other._array.Clone();
    }

    public int CompareTo(object? other, IComparer comparer)
        => ((IStructuralComparable)_array).CompareTo(other, comparer);

    public bool Equals(object? other, IEqualityComparer comparer)
        => ((IStructuralEquatable)_array).Equals(other, comparer);

    public int GetHashCode(IEqualityComparer comparer)
        => ((IStructuralEquatable)_array).GetHashCode(comparer);

    public object Clone()
        => new EnumDictionary<TKey, TValue>(this);

    public bool ContainsKey(TKey key)
        => true;

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = this[key];
        return true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var key in s_keys) {
            yield return KeyValuePair.Create(key, this[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}