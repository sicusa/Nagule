namespace Nagule;

public interface IProfilingEventContext : IEventContext
{
    IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>> Profiles { get; }
    IReadOnlyDictionary<object, LayerProfile>? GetProfiles<TListener>();
    void TriggerMonitorableEvent<TListener>(Action<TListener> action);
}