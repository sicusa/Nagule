namespace Nagule;

using Sia;

public static class ObjectEvents
{
    public class Destroy : SingletonEvent<Destroy>, ICancellableEvent {}
    internal class DestroyImmediately : SingletonEvent<DestroyImmediately> {}
}