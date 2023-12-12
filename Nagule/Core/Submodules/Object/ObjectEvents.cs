namespace Nagule;

using Sia;

public static class ObjectEvents
{
    public class Destroy : SingletonEvent<Destroy>, ICancellableEvent {}
    public class DestroyImmediately : SingletonEvent<DestroyImmediately> {}
}