namespace Nagule;

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Aeco;

public class ProfilingEventContext : EventContext, IProfilingEventContext
{
    public IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>> Profiles
        => (IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>>)_profiles;

    private Dictionary<Type, Dictionary<object, LayerProfile>> _profiles = new();
    private Stopwatch _stopwatch = new();

    public ProfilingEventContext(params ILayer<IComponent>[] sublayers)
        : base(sublayers)
    {
    }

    public virtual void TriggerMonitorableEvent<TListener>(Action<TListener> action)
    {
        var profileListeners = GetListeners<IProfileListener>();

        if (!_profiles.TryGetValue(typeof(TListener), out var profiles)) {
            profiles = new();
            _profiles.Add(typeof(TListener), profiles);
        }
        
        foreach (var listener in GetListeners<TListener>()) {
            try {
                _stopwatch.Restart();
                action(listener);
                _stopwatch.Stop();
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke {typeof(TListener)} method for {listener}: " + e);
            }

            var time = _stopwatch.Elapsed.TotalSeconds;
            ref var profile = ref CollectionsMarshal.GetValueRefOrAddDefault(
                profiles, listener!, out bool exists);

            if (!exists) {
                profile.InitialElapsedTime = time;
                profile.InitialUpdateFrame = UpdateFrame;
                profile.InitialRenderFrame = RenderFrame;
            }

            profile.CurrentElapsedTime = time;
            profile.CurrentUpdateFrame = UpdateFrame;
            profile.CurrentRenderFrane = RenderFrame;

            profile.MaximumElapsedTime = Math.Max(profile.MaximumElapsedTime, time);
            profile.MinimumElapsedTime = profile.MinimumElapsedTime == 0 ? time : Math.Min(profile.MinimumElapsedTime, time);
            profile.AverangeElapsedTime = (profile.AverangeElapsedTime + time) / 2.0;

            foreach (var profileListener in profileListeners)  {
                profileListener.OnProfile(listener!, in profile);
            }
        }
    }

    public void ClearProfiles()
    {
        _profiles.Clear();
    }

    public IReadOnlyDictionary<object, LayerProfile>? GetProfiles<TListener>()
        => _profiles.TryGetValue(typeof(TListener), out var profiles) ? profiles : null;

    public override void Update(float deltaTime)
    {
        Time += deltaTime;
        ++UpdateFrame;
        TriggerMonitorableEvent<IUpdateListener>(l => l.OnUpdate(this, deltaTime));
        TriggerMonitorableEvent<IEngineUpdateListener>(l => l.OnEngineUpdate(this, deltaTime));
        TriggerMonitorableEvent<ILateUpdateListener>(l => l.OnLateUpdate(this, deltaTime));
    }

    public override void Render(float deltaTime)
    {
        ++RenderFrame;
        TriggerMonitorableEvent<IRenderListener>(l => l.OnRender(this, deltaTime));
        TriggerMonitorableEvent<IRenderFinishedListener>(l => l.OnRenderFinished(this, deltaTime));
    }
}