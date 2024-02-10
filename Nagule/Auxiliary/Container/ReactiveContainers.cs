namespace Nagule;

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Reactive.Subjects;
using CommunityToolkit.HighPerformance;

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

    private readonly Subject<T> _subject = new();
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
    public readonly void ApplyTo(IList<T> list)
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

    public readonly void ApplyTo<TTarget>(IList<TTarget> list, Func<T, TTarget> mapper)
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

    public int Count => Raw.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)Raw).IsReadOnly;

    public T this[int index] {
        get => Raw[index];
        set {
            if (_replaceSubject != null) {
                var prev = Raw[index];
                Raw[index] = value;
                _replaceSubject.OnNext((index, prev, value));
            }
            else {
                Raw[index] = value;
            }
            _subject.OnNext(new(ReactiveListOperation.Set, index, value));
        }
    }

    public List<T> Raw { get; } = [];

    private readonly Subject<ReactiveListEvent<T>> _subject = new();
    private Subject<(int, T, T)>? _replaceSubject;

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveListEvent<T> tuple)
    {
        var (op, index, value) = tuple;

        if (op == ReactiveListOperation.Set) {
            Raw[index] = value;
        }
        else if (op == ReactiveListOperation.Insert) {
            Raw.Insert(index, value);
        }
        else {
            if (index < 0 || index >= Raw.Count) {
                throw new IndexOutOfRangeException("Index out of range");
            }
            if (!EqualityComparer<T>.Default.Equals(value, Raw[index])) {
                throw new InvalidOperationException("Value conflict");
            }
            Raw.RemoveAt(index);
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
        => Raw.IndexOf(item);

    public void Add(T item)
    {
        Raw.Add(item);
        _subject.OnNext(new(ReactiveListOperation.Insert, Raw.Count - 1, item));
    }

    public void AddRange(IEnumerable<T> items)
    {
        int i = Raw.Count;
        Raw.AddRange(items);

        foreach (var item in items) {
            _subject.OnNext(new(ReactiveListOperation.Insert, i, item));
            ++i;
        }
    }

    public void Insert(int index, T item)
    {
        Raw.Insert(index, item);
        _subject.OnNext(new(ReactiveListOperation.Insert, index, item));
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Raw.Count) {
            throw new IndexOutOfRangeException("Index out of range");
        }
        var value = Raw[index];
        Raw.RemoveAt(index);
        _subject.OnNext(new(ReactiveListOperation.Remove, index, value));
    }

    public void Clear()
    {
        int i = 0;
        foreach (ref var item in Raw.AsSpan()) {
            _subject.OnNext(new(ReactiveListOperation.Remove, i, item));
            ++i;
        }
        Raw.Clear();
    }

    public bool Contains(T item)
        => Raw.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => Raw.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        var index = Raw.IndexOf(item);
        if (index == -1) {
            return false;
        }
        var value = Raw[index];
        Raw.RemoveAt(index);
        _subject.OnNext(new(ReactiveListOperation.Remove, index, value));
        return true;
    }

    public List<T>.Enumerator GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Raw.GetEnumerator();
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
    public readonly void ApplyTo(ISet<T> set)
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

    public readonly void ApplyTo<TTarget>(ISet<TTarget> set, Func<T, TTarget> mapper)
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

    public int Count => Raw.Count;
    bool ICollection<T>.IsReadOnly => ((ICollection<T>)Raw).IsReadOnly;

    public HashSet<T> Raw { get; } = [];

    private readonly Subject<ReactiveSetEvent<T>> _subject = new();

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveSetEvent<T> tuple)
    {
        var (op, value) = tuple;

        if (op == ReactiveSetOperation.Add) {
            if (!Raw.Add(value)) {
                return;
            }
        }
        else {
            if (!Raw.Remove(value)) {
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
        if (!Raw.Add(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveSetOperation.Add, item));
        return true;
    }

    void ICollection<T>.Add(T item)
        => Add(item);

    public bool Remove(T item)
    {
        if (!Raw.Remove(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveSetOperation.Remove, item));
        return true;
    }

    public void Clear()
    {
        foreach (var item in Raw) {
            _subject.OnNext(new(ReactiveSetOperation.Remove, item));
        }
        Raw.Clear();
    }

    public bool Contains(T item)
        => Raw.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => Raw.CopyTo(array, arrayIndex);

    public HashSet<T>.Enumerator GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)Raw).GetEnumerator();

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

        Raw.RemoveWhere(item => {
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
        
        Raw.RemoveWhere(item => {
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
        => Raw.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other)
        => Raw.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other)
        => Raw.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other)
        => Raw.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other)
        => Raw.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other)
        => Raw.SetEquals(other);
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
    public readonly void ApplyTo(IDictionary<TKey, TValue> dict)
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

    public readonly void ApplyTo<TTargetKey, TTargetValue>(
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

    public Dictionary<TKey, TValue>.KeyCollection Keys => Raw.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => Raw.Values;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Raw.Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Raw.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Raw.Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => Raw.Values;

    public int Count => Raw.Count;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        => ((ICollection<KeyValuePair<TKey, TValue>>)Raw).IsReadOnly;

    public TValue this[TKey key] {
        get => Raw[key];
        set {
            ref var valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(Raw, key, out bool exists);
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

    public Dictionary<TKey, TValue> Raw { get; } = [];

    private readonly Subject<ReactiveDictionaryEvent<TKey, TValue>> _subject = new();
    private Subject<(TKey, TValue, TValue)>? _replaceSubject;

    public override void Dispose()
        => _subject.Dispose();

    public override void OnNext(ReactiveDictionaryEvent<TKey, TValue> tuple)
    {
        var (op, key, value) = tuple;

        if (op == ReactiveDictionaryOperation.Set) {
            Raw[key] = value;
        }
        else {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)Raw)
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
        Raw.Add(key, value);
        _subject.OnNext(new(ReactiveDictionaryOperation.Set, key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item)
        => Add(item.Key, item.Value);

    public bool ContainsKey(TKey key)
        => Raw.ContainsKey(key);

    public bool Remove(TKey key)
    {
        if (!Raw.Remove(key, out var value)) {
            return false;
        }
        _subject.OnNext(new(ReactiveDictionaryOperation.Remove, key, value));
        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!((ICollection<KeyValuePair<TKey, TValue>>)Raw).Remove(item)) {
            return false;
        }
        _subject.OnNext(new(ReactiveDictionaryOperation.Remove, item.Key, item.Value));
        return true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => Raw.TryGetValue(key, out value);

    public void Clear()
    {
        foreach (var p in Raw) {
            _subject.OnNext(new(ReactiveDictionaryOperation.Remove, p.Key, p.Value));
        }
        Raw.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
        => Raw.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)Raw).CopyTo(array, arrayIndex);

    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => Raw.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)Raw).GetEnumerator();
}

#endregion