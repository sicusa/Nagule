namespace Nagule;

public interface IProfilingContext : IContext
{
    IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>> Profiles { get; }
    IReadOnlyDictionary<object, LayerProfile>? GetProfiles<TListener>();

    void TriggerProfilingEvent<TListener>(Action<TListener> action);
    void TriggerProfilingEvent<TListener>(ReadOnlySpan<TListener> listeners, Action<TListener> action);
}