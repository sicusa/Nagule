namespace Nagule;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

using Sia;

public class Profiler : IAddon
{
    private class ProfileScope : IDisposable
    {
        private static readonly Stack<ProfileScope> s_pool = new();

        public Profiler? _profiler;
        public string _path = "";

        private readonly Stopwatch _stopwatch = new();
        private double _elapsedTime;

        public static ProfileScope Create(Profiler profiler, string path)
        {
            if (!s_pool.TryPop(out var scope)) {
                scope = new ProfileScope();
            }
            scope.Start(profiler, path);
            return scope;
        }

        public void Start(Profiler profiler, string path)
        {
            _profiler = profiler;
            _path = path;
            _stopwatch.Restart();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _elapsedTime = _stopwatch.Elapsed.TotalSeconds;

            ref var profile = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _profiler!._profiles, _path, out bool exists);

            if (exists) {
                profile.MaximumElapsedTime = Math.Max(profile.MaximumElapsedTime, _elapsedTime);
                profile.MinimumElapsedTime = Math.Min(profile.MinimumElapsedTime, _elapsedTime);
                profile.AverangeElapsedTime = (profile.AverangeElapsedTime + _elapsedTime) / 2;
            }
            else {
                profile.InitialTime = _profiler.Time;
                profile.InitialFrame = _profiler.Frame;
                profile.InitialElapsedTime = _elapsedTime;

                profile.MaximumElapsedTime = _elapsedTime;
                profile.MinimumElapsedTime = _elapsedTime;
                profile.AverangeElapsedTime = _elapsedTime;
            }

            profile.CurrentElapsedTime = _elapsedTime;
            profile.CurrentFrame = _profiler.Frame;

            if (_profiler._profileSubjects.TryGetValue(_path, out var subject)) {
                subject.OnNext(profile);
            }

            _profiler = null;
            _path = "";

            s_pool.Push(this);
        }
    }

    public long Frame { get; internal set; }
    public double Time { get; internal set; }

    public IEnumerable<KeyValuePair<string, Profile>> Profiles => _profiles;

    private readonly Dictionary<string, Profile> _profiles = [];
    private readonly Dictionary<string, Subject<Profile>> _profileSubjects = [];
    private readonly Dictionary<(string, object), string> _profileKeys = [];

    public Profile? GetProfile(string path)
        => _profiles.TryGetValue(path, out var profile) ? profile : null;

    public Profile? GetProfile(string category, object target)
        => _profiles.TryGetValue(
                GetProfileKey(category, target), out var profile)
            ? profile : null;
        
    public IObservable<Profile> ObserveProfile(string path)
    {
        ref var subject = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _profileSubjects, path, out bool exists);
        if (!exists) {
            subject = new();
        }
        return subject!;
    }

    public IObservable<Profile> ObserveProfile(string category, object target)
        => ObserveProfile(GetProfileKey(category, target));
    
    public bool RemoveProfile(string path)
        => _profiles.Remove(path, out var _);
    
    public bool RemoveProfile(string path, [MaybeNullWhen(false)] out Profile profile)
        => _profiles.Remove(path, out profile);

    public void ClearProfiles()
        => _profiles.Clear();

    public IDisposable Profile(string path)
        => ProfileScope.Create(this, path);

    public IDisposable Profile(string category, object target)
        => ProfileScope.Create(this, GetProfileKey(category, target));

    private string GetProfileKey(string category, object target)
    {
        ref var key = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _profileKeys, (category, target), out bool exists);
        if (!exists) {
            key = category + '/' + target;
        }
        return key!;
    }
}