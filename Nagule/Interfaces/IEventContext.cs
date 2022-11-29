namespace Nagule;

public interface IEventContext : IContext
{
    ReadOnlySpan<TListener> GetListeners<TListener>();
}