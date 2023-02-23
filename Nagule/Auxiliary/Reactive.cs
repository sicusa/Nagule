namespace Nagule;

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

#region PropertyObject

public class ReactiveObject<T> : SubjectBase<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public T Value {
        get => _value;
        set => OnNext(value);
    }

    private Subject<T> _subject = new();
    private T _value;

    public ReactiveObject(T value)
    {
        _value = value;
    }

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(T value)
    {
        _subject.OnNext(value);
        _value = value;
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<T> observer)
        => _subject.Subscribe(observer);
    
    public static implicit operator ReactiveObject<T>(T value) => new(value);
    public static implicit operator T(ReactiveObject<T> prop) => prop._value;
}

#endregion

#region ReactiveList

public enum ReactiveListOperation
{
    Set,
    Insert,
    Remove
}

public record struct ReactiveListEvent<T>(
    ReactiveListOperation Operation, int Index, T Value);

public class ReactiveList<T>
    : SubjectBase<ReactiveListEvent<T>>, IList<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public int Count => _list.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

    public T this[int index] {
        get => _list[index];
        set {
            _list[index] = value;
            _subject.OnNext(new(ReactiveListOperation.Set, index, value));
        }
    }

    private Subject<ReactiveListEvent<T>> _subject = new();
    private List<T> _list = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveListEvent<T> tuple)
    {
        var (op, index, value) = tuple;

        if (op == ReactiveListOperation.Set) {
            _list[index] = value;
        }
        else if (op == ReactiveListOperation.Insert) {
            _list.Insert(index, value);
        }
        else {
            if (index < 0 || index >= _list.Count) {
                throw new IndexOutOfRangeException("Index out of range");
            }
            if (!EqualityComparer<T>.Default.Equals(value, _list[index])) {
                throw new InvalidOperationException("Value conflict");
            }
            _list.RemoveAt(index);
        }

        _subject.OnNext(tuple);
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<ReactiveListEvent<T>> observer)
        => _subject.Subscribe(observer);

    public int IndexOf(T item)
        => _list.IndexOf(item);

    public void Add(T item)
    {
        _list.Add(item);
        _subject.OnNext(new(ReactiveListOperation.Insert, _list.Count - 1, item));
    }

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        _subject.OnNext(new(ReactiveListOperation.Insert, index, item));
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _list.Count) {
            throw new IndexOutOfRangeException("Index out of range");
        }
        var value = _list[index];
        _list.RemoveAt(index);
        _subject.OnNext(new(ReactiveListOperation.Remove, index, value));
    }

    public void Clear()
    {
        int i = 0;
        foreach (ref var item in CollectionsMarshal.AsSpan(_list)) {
            _subject.OnNext(new(ReactiveListOperation.Remove, i, item));
            ++i;
        }
        _list.Clear();
    }

    public bool Contains(T item)
        => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        var index = _list.IndexOf(item);
        if (index == -1) {
            return false;
        }
        var value = _list[index];
        _list.RemoveAt(index);
        _subject.OnNext(new(ReactiveListOperation.Remove, index, value));
        return true;
    }

    public IEnumerator<T> GetEnumerator()
        => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _list.GetEnumerator();
}

#endregion

#region ReactiveHashSet

public enum ReactiveHashSetOperation
{
    Add,
    Remove
}

public record struct ReactiveHashSetEvent<T>(
    ReactiveHashSetOperation Operation, T Value);

public class HashSetProperty<T>
    : SubjectBase<ReactiveHashSetEvent<T>>, ISet<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public int Count => _set.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)_set).IsReadOnly;

    private Subject<ReactiveHashSetEvent<T>> _subject = new();
    private HashSet<T> _set = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveHashSetEvent<T> tuple)
    {
        var (op, value) = tuple;

        if (op == ReactiveHashSetOperation.Add) {
            if (!_set.Add(value)) {
                return;
            }
        }
        else {
            if (!_set.Remove(value)) {
                return;
            }
        }
        
        _subject.OnNext(tuple);
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<ReactiveHashSetEvent<T>> observer)
        => _subject.Subscribe(observer);

    public bool Add(T item)
    {
        if (!_set.Add(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveHashSetOperation.Add, item));
        return true;
    }

    void ICollection<T>.Add(T item)
        => Add(item);

    public bool Remove(T item)
    {
        if (!_set.Remove(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveHashSetOperation.Remove, item));
        return true;
    }

    public void Clear()
    {
        foreach (var item in _set) {
            _subject.OnNext(new(ReactiveHashSetOperation.Remove, item));
        }
        _set.Clear();
    }

    public bool Contains(T item)
        => _set.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _set.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator()
        => _set.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_set).GetEnumerator();

    public void UnionWith(IEnumerable<T> other)
    {
        foreach (var item in other) {
            Add(item);
        }
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        foreach (var item in other) {
            Remove(item);
        }
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        var otherSet = other as ISet<T> ?? new HashSet<T>(other);

        _set.RemoveWhere(item => {
            if (otherSet.Contains(item)) {
                return false;
            }
            _subject.OnNext(new(ReactiveHashSetOperation.Remove, item));
            return true;
        });
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        var otherSet = new HashSet<T>(other);
        
        _set.RemoveWhere(item => {
            if (otherSet.Remove(item)) {
                return false;
            }
            _subject.OnNext(new(ReactiveHashSetOperation.Remove, item));
            return true;
        });

        foreach (var item in otherSet) {
            Add(item);
        }
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
        => _set.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other)
        => _set.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other)
        => _set.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other)
        => _set.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other)
        => _set.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other)
        => _set.SetEquals(other);
}

#endregion

#region ReactiveDictionary

public enum ReactiveDictionaryOperation
{
    Set,
    Remove
}

public record struct ReactiveDictionaryEvent<TKey, TValue>(
    ReactiveDictionaryOperation Operation, TKey Key, TValue Value);

public class ReactiveDictionary<TKey, TValue>
    : SubjectBase<ReactiveDictionaryEvent<TKey, TValue>>, IDictionary<TKey, TValue>
    where TKey : notnull
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;

    public int Count => _dict.Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).IsReadOnly;

    public TValue this[TKey key] {
        get => _dict[key];
        set {
            _dict[key] = value;
            _subject.OnNext(new(ReactiveDictionaryOperation.Set, key, value));
        }
    }

    private Subject<ReactiveDictionaryEvent<TKey, TValue>> _subject = new();
    private Dictionary<TKey, TValue> _dict = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveDictionaryEvent<TKey, TValue> tuple)
    {
        var (op, key, value) = tuple;

        if (op == ReactiveDictionaryOperation.Set) {
            _dict[key] = value;
        }
        else {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)_dict)
                    .Remove(KeyValuePair.Create(key, value))) {
                throw new InvalidOperationException("Value conflict");
            }
        }
        
        _subject.OnNext(tuple);
    }

    public override void OnCompleted()
        => _subject.OnCompleted();

    public override void OnError(Exception error)
        => _subject.OnError(error);

    public override IDisposable Subscribe(IObserver<ReactiveDictionaryEvent<TKey, TValue>> observer)
        => _subject.Subscribe(observer);

    public void Add(TKey key, TValue value)
    {
        _dict.Add(key, value);
        _subject.OnNext(new(ReactiveDictionaryOperation.Set, key, value));
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
        _subject.OnNext(new(ReactiveDictionaryOperation.Remove, key, value));
        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveDictionaryOperation.Remove, item.Key, item.Value));
        return true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _dict.TryGetValue(key, out value);

    public void Clear()
    {
        foreach (var p in _dict) {
            _subject.OnNext(new(ReactiveDictionaryOperation.Remove, p.Key, p.Value));
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

#endregion