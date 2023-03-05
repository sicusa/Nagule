namespace Nagule;

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Reactive.Subjects;

#region ReactiveObject

public class ReactiveObject<T> : SubjectBase<T>
{
    public IObservable<(T?, T)> Modified => _modifySubject ??= new();

    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public T Value {
        get => _set ? _value : throw new InvalidOperationException("Reactive object not set");
        set => OnNext(value);
    }

    public ref T Raw => ref _value;

    private Subject<T> _subject = new();
    private Subject<(T?, T)>? _modifySubject;

    private T _value;
    private bool _set;

    public ReactiveObject()
    {
        _value = default!;
        _set = false;
    }

    public ReactiveObject(T value)
    {
        _value = value;
        _set = true;
    }

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(T value)
    {
        if (_set && _modifySubject != null) {
            var prev = _value;
            _value = value;
            _subject.OnNext(value);
            _modifySubject.OnNext((prev, value));
        }
        else {
            _set = true;
            _value = value;
            _subject.OnNext(value);
        }
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
    ReactiveListOperation Operation, int Index, T Value)
{
    public void ApplyTo(IList<T> list)
    {
        switch (Operation) {
        case ReactiveListOperation.Set:
            list[Index] = Value;
            break;
        case ReactiveListOperation.Insert:
            list.Insert(Index, Value);
            break;
        case ReactiveListOperation.Remove:
            list.RemoveAt(Index);
            break;
        }
    }

    public void ApplyTo<TTarget>(IList<TTarget> list, Func<T, TTarget> mapper)
    {
        switch (Operation) {
        case ReactiveListOperation.Set:
            list[Index] = mapper(Value);
            break;
        case ReactiveListOperation.Insert:
            list.Insert(Index, mapper(Value));
            break;
        case ReactiveListOperation.Remove:
            list.RemoveAt(Index);
            break;
        }
    }
}

public class ReactiveList<T>
    : SubjectBase<ReactiveListEvent<T>>, IList<T>, IReadOnlyList<T>
{
    public IObservable<(int, T, T)> Replaced => _replaceSubject ??= new();

    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public int Count => _list.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

    public T this[int index] {
        get => _list[index];
        set {
            if (_replaceSubject != null) {
                var prev = _list[index];
                _list[index] = value;
                _replaceSubject.OnNext((index, prev, value));
            }
            else {
                _list[index] = value;
            }
            _subject.OnNext(new(ReactiveListOperation.Set, index, value));
        }
    }

    public List<T> Raw => _list;

    private Subject<ReactiveListEvent<T>> _subject = new();
    private Subject<(int, T, T)>? _replaceSubject;
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

    public void AddRange(IEnumerable<T> items)
    {
        int i = _list.Count;
        _list.AddRange(items);

        foreach (var item in items) {
            _subject.OnNext(new(ReactiveListOperation.Insert, i, item));
            ++i;
        }
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

    public List<T>.Enumerator GetEnumerator()
        => _list.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _list.GetEnumerator();
}

#endregion

#region ReactiveHashSet

public enum ReactiveSetOperation
{
    Add,
    Remove
}

public record struct ReactiveSetEvent<T>(
    ReactiveSetOperation Operation, T Value)
{
    public void ApplyTo(ISet<T> set)
    {
        switch (Operation) {
        case ReactiveSetOperation.Add:
            set.Add(Value);
            break;
        case ReactiveSetOperation.Remove:
            set.Remove(Value);
            break;
        }
    }

    public void ApplyTo<TTarget>(ISet<TTarget> set, Func<T, TTarget> mapper)
    {
        switch (Operation) {
        case ReactiveSetOperation.Add:
            set.Add(mapper(Value));
            break;
        case ReactiveSetOperation.Remove:
            set.Remove(mapper(Value));
            break;
        }
    }
}

public class ReactiveHashSet<T>
    : SubjectBase<ReactiveSetEvent<T>>, ISet<T>, IReadOnlySet<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public int Count => _set.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)_set).IsReadOnly;

    public HashSet<T> Raw => _set;

    private Subject<ReactiveSetEvent<T>> _subject = new();
    private HashSet<T> _set = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveSetEvent<T> tuple)
    {
        var (op, value) = tuple;

        if (op == ReactiveSetOperation.Add) {
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

    public override IDisposable Subscribe(IObserver<ReactiveSetEvent<T>> observer)
        => _subject.Subscribe(observer);

    public bool Add(T item)
    {
        if (!_set.Add(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveSetOperation.Add, item));
        return true;
    }

    void ICollection<T>.Add(T item)
        => Add(item);

    public bool Remove(T item)
    {
        if (!_set.Remove(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveSetOperation.Remove, item));
        return true;
    }

    public void Clear()
    {
        foreach (var item in _set) {
            _subject.OnNext(new(ReactiveSetOperation.Remove, item));
        }
        _set.Clear();
    }

    public bool Contains(T item)
        => _set.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _set.CopyTo(array, arrayIndex);

    public HashSet<T>.Enumerator GetEnumerator()
        => _set.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
            _subject.OnNext(new(ReactiveSetOperation.Remove, item));
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
            _subject.OnNext(new(ReactiveSetOperation.Remove, item));
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
    ReactiveDictionaryOperation Operation, TKey Key, TValue Value)
{
    public void ApplyTo(IDictionary<TKey, TValue> dict)
    {
        switch (Operation) {
        case ReactiveDictionaryOperation.Set:
            dict[Key] = Value;
            break;
        case ReactiveDictionaryOperation.Remove:
            dict.Remove(KeyValuePair.Create(Key, Value));
            break;
        }
    }

    public void ApplyTo<TTargetKey, TTargetValue>(
        IDictionary<TTargetKey, TTargetValue> dict, Func<TKey, TTargetKey> keyMapper, Func<TValue, TTargetValue> valueMapper)
    {
        switch (Operation) {
        case ReactiveDictionaryOperation.Set:
            dict[keyMapper(Key)] = valueMapper(Value);
            break;
        case ReactiveDictionaryOperation.Remove:
            dict.Remove(KeyValuePair.Create(keyMapper(Key), valueMapper(Value)));
            break;
        }
    }
}

public class ReactiveDictionary<TKey, TValue>
    : SubjectBase<ReactiveDictionaryEvent<TKey, TValue>>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public IObservable<(TKey, TValue, TValue)> Replaced => _replaceSubject ??= new();

    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public Dictionary<TKey, TValue>.KeyCollection Keys => _dict.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => _dict.Values;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dict.Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dict.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dict.Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => _dict.Values;

    public int Count => _dict.Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).IsReadOnly;

    public TValue this[TKey key] {
        get => _dict[key];
        set {
            ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, key, out bool exists);
            if (exists && _replaceSubject != null) {
                var prev = valueRef;
                valueRef = value;
                _replaceSubject.OnNext((key, prev!, value));
            }
            else {
                valueRef = value;
            }
            _subject.OnNext(new(ReactiveDictionaryOperation.Set, key, value));
        }
    }

    public Dictionary<TKey, TValue> Raw => _dict;

    private Subject<ReactiveDictionaryEvent<TKey, TValue>> _subject = new();
    private Subject<(TKey, TValue, TValue)>? _replaceSubject;

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

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_dict).GetEnumerator();
}

#endregion