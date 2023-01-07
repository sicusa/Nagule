namespace Nagule;

public interface IProfilingContext : IContext
{
    IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>> Profiles { get; }
    IReadOnlyDictionary<object, LayerProfile>? GetProfiles<TListener>();
    void TriggerMonitorableEvent<TListener>(Action<TListener> action);
}