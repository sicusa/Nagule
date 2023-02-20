namespace Nagule.Graphics;

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

public class Property<T> : SubjectBase<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public T Value {
        get => _set ? _value : throw new InvalidDataException("Property not set");
        set => OnNext(value);
    }

    private Subject<T> _subject = new();
    private T _value;
    private bool _set;

    public Property()
    {
        _value = default!;
        _set = false;
    }

    public Property(T value)
    {
        _value = value;
        _set = true;
    }

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(T value)
    {
        _subject.OnNext(value);
        _value = value;
        _set = true;
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<T> observer)
        => _subject.Subscribe(observer);
    
    public static implicit operator Property<T>(T value) => new(value);
    public static implicit operator T(Property<T> prop) => prop._value;
}

public enum DictionaryPropertyOperation
{
    Set,
    Remove
}

public class DictionaryProperty<TKey, TValue>
    : SubjectBase<(DictionaryPropertyOperation, TKey, TValue)>, IDictionary<TKey, TValue>
    where TKey : notnull
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;

    public int Count => _dict.Count;
    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).IsReadOnly;

    public TValue this[TKey key] {
        get => _dict[key];
        set {
            _dict[key] = value;
            _subject.OnNext((DictionaryPropertyOperation.Set, key, value));
        }
    }

    private Subject<(DictionaryPropertyOperation, TKey, TValue)> _subject = new();
    private Dictionary<TKey, TValue> _dict = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext((DictionaryPropertyOperation, TKey, TValue) tuple)
    {
        var (op, key, value) = tuple;

        if (op == DictionaryPropertyOperation.Set) {
            _dict[key] = value;
        }
        else {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)_dict)
                    .Remove(KeyValuePair.Create(key, value))) {
                throw new KeyNotFoundException("Value conflict");
            }
        }
        
        _subject.OnNext(tuple);
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<(DictionaryPropertyOperation, TKey, TValue)> observer)
        => _subject.Subscribe(observer);

    public void Add(TKey key, TValue value)
    {
        _dict.Add(key, value);
        _subject.OnNext((DictionaryPropertyOperation.Set, key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    public bool ContainsKey(TKey key)
        => _dict.ContainsKey(key);

    public bool Remove(TKey key)
    {
        if (!_dict.Remove(key, out var value)) {
            return false;
        }
        _subject.OnNext((DictionaryPropertyOperation.Remove, key, value));
        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item)) {
            return false;
        }
        _subject.OnNext((DictionaryPropertyOperation.Remove, item.Key, item.Value));
        return true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _dict.TryGetValue(key, out value);

    public void Clear()
    {
        foreach (var p in _dict) {
            _subject.OnNext((DictionaryPropertyOperation.Remove, p.Key, p.Value));
        }
        _dict.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _dict.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<TKey, TValue>>)_dict).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_dict).GetEnumerator();
}