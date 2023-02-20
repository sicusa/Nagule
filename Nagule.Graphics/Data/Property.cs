namespace Nagule.Graphics;

using System;
using System.Reactive.Subjects;

public class Property<T> : SubjectBase<T>
{
    public override bool HasObservers => _subject.HasObservers;
    public override bool IsDisposed => _subject.IsDisposed;

    public T Value {
        get => _value;
        set => OnNext(value);
    }

    private Subject<T> _subject = new();
    private T _value;

    public Property(T value)
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
    
    public static implicit operator Property<T>(T value) => new(value);
    public static implicit operator T(Property<T> prop) => prop._value;
}