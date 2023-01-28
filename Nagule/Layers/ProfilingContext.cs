namespace Nagule;

using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Aeco;

public class ProfilingContext : Context, IProfilingContext
{
    public IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>> Profiles
        => (IEnumerable<KeyValuePair<Type, IReadOnlyDictionary<object, LayerProfile>>>)_profiles;

    private ConcurrentDictionary<Type, Dictionary<object, LayerProfile>> _profiles = new();
    private Stopwatch _stopwatch = new();

    public ProfilingContext(params ILayer<IComponent>[] sublayers)
        : base(sublayers)
    {
    }

    public virtual void TriggerMonitorableEvent<TListener>(Action<TListener> action)
    {
        var profileListeners = GetListeners<IProfileListener>();
        var type = typeof(TListener);

        if (!_profiles.TryGetValue(type, out var profiles)) {
            profiles = _profiles.AddOrUpdate(
                typeof(TListener), _ => new(), (_, value) => value);
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
            
            if (time > 0.01f) {
                //Console.WriteLine($"Alert: {typeof(TListener)} {listener} ({time})");
            }

            if (!exists) {
                profile.InitialElapsedTime = time;
                profile.InitialUpdateFrame = UpdateFrame;
                profile.InitialRenderFrame = RenderFrame;
            }

            profile.CurrentElapsedTime = time;
            profile.CurrentUpdateFrame = UpdateFrame;
            profile.CurrentRenderFrame = RenderFrame;

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
        ++UpdateFrame;
        Time += deltaTime;
        DeltaTime = deltaTime;

        SubmitBatchedCommands();

        TriggerMonitorableEvent<IFrameStartListener>(l => l.OnFrameStart(this));
        TriggerMonitorableEvent<IUpdateListener>(l => l.OnUpdate(this));
        TriggerMonitorableEvent<IEngineUpdateListener>(l => l.OnEngineUpdate(this));
        TriggerMonitorableEvent<ILateUpdateListener>(l => l.OnLateUpdate(this));
    }

    public override void Render(float deltaTime)
    {
        ++RenderFrame;
        RenderTime += deltaTime;
        RenderDeltaTime = deltaTime;

        TriggerMonitorableEvent<IRenderListener>(l => l.OnRender(this));
    }
}