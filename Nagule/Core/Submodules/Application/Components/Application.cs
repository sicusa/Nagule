namespace Nagule;

using Sia;

public record struct Application
{
    public class Start : SingletonEvent<Start> {}
    public class Quit : SingletonEvent<Quit>, ICancellableEvent {}
}