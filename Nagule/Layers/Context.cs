namespace Nagule;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

using Aeco;
using Aeco.Local;
using Aeco.Reactive;

public class Context : CompositeLayer, IContext
{
    private class CommandTarget
    {
        public BlockingCollection<ICommand> Collection { get; } = new();

        private BatchedCommand _batch = BatchedCommand.Create();

        public void AddBatchedCommand(ICommand command)
        {
            lock (_batch) {
                _batch.Commands.Add(command);
            }
        }

        public void SubmitBatch()
        {
            BatchedCommand batch;

            lock (_batch) {
                batch = _batch;
                _batch = BatchedCommand.Create();
            }

            if (batch.Commands.Count != 0) {
                Collection.Add(batch);
            }
        }
    }

    private class ProfileScope : IDisposable
    {
        private static ConcurrentStack<ProfileScope> s_pool = new();

        public Context? _context;
        public string _path = "";

        private Stopwatch _stopwatch = new();
        private double _time;

        public static ProfileScope Create(Context context, string path)
        {
            if (!s_pool.TryPop(out var scope)) {
                scope = new ProfileScope();
            }
            scope.Start(context, path);
            return scope;
        }

        public void Start(Context context, string path)
        {
            _context = context;
            _path = path;
            _stopwatch.Restart();
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            _time = _stopwatch.Elapsed.TotalSeconds;
            _context!._profiles.AddOrUpdate(_path, CreateProfile, UpdateProfile);

            _context = null;
            _path = "";

            s_pool.Push(this);
        }

        private Profile CreateProfile(string path)
            => new Profile {
                InitialElapsedTime = _time,
                InitialFrame = _context!.Frame,

                CurrentElapsedTime = _time,
                CurrentFrame = _context!.Frame,

                MaximumElapsedTime = _time,
                MinimumElapsedTime = _time,
                AverangeElapsedTime = _time
            };
        
        private Profile UpdateProfile(string path, Profile prev)
            => new Profile {
                InitialElapsedTime = prev.InitialElapsedTime,
                InitialFrame = prev.InitialFrame,

                CurrentElapsedTime = _time,
                CurrentFrame = _context!.Frame,

                MaximumElapsedTime = Math.Max(prev.MaximumElapsedTime, _time),
                MinimumElapsedTime = Math.Min(prev.MinimumElapsedTime, _time),
                AverangeElapsedTime = Math.Round((prev.AverangeElapsedTime + _time) / 2.0, 7)
            };
    }

    public IEnumerable<KeyValuePair<string, Profile>> Profiles => _profiles;
    
    public bool Running { get; protected set; }
    public float Time { get; protected set; }
    public float DeltaTime { get; protected set; }
    public long Frame { get; protected set; }

    private ConcurrentDictionary<Type, object> _listeners = new();
    private ConcurrentDictionary<Type, CommandTarget> _commandTargets = new();
    private ConcurrentDictionary<string, Profile> _profiles = new();
    private ConcurrentDictionary<(string, object), string> _profileKeys = new();

    public Context(params ILayer<IComponent>[] sublayers)
    {
        InternalAddSublayers(
            new DestroyedObjectCleaner(),
            new UnusedResourceDestroyer(),
            
            new UpdateCommandExecutor(),
            new NameRegisterer(),
            new TransformUpdator());

        InternalAddSublayers(sublayers);

        var eventStorage = new PolyTagStorage<IReactiveEvent>();
        var anyEventStorage = new PolySingletonStorage<IAnyReactiveEvent>();

        InternalAddSublayers(
            new AutoClearer(eventStorage),
            new AutoClearer(anyEventStorage),

            eventStorage,
            anyEventStorage,

            new ReactiveCompositeLayer(
                new PolySingletonStorage<IReactiveSingletonComponent>(),
                new PolyTagStorage<IReactiveTagComponent>(),
                new PolyDenseStorage<IReactiveComponent>()) {
                EventDataLayer = eventStorage,
                AnyEventDataLayer = anyEventStorage
            },

            new PolySingletonStorage<ISingletonComponent>(),
            new PolyTagStorage<ITagComponent>(),
            new PolyDenseStorage<IPooledComponent>()
        );
    }

    public virtual void Load()
    {
        Running = true;

        foreach (var listener in GetSublayersRecursively<ILoadListener>()) {
            try {
                using (Profile("Load", listener)) {
                    listener.OnLoad(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILoadListener method for {listener}: " + e);
            }
        }

        InvokeFrameStartListeners();
        InvokeInternalFrameListeners();
    }

    public virtual void Unload()
    {
        if (!Running) { return; }
        Running = false;

        foreach (var listener in GetSublayersRecursively<IUnloadListener>()) {
            try {
                listener.OnUnload(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUnloadListener method for {listener}: " + e);
            }
        }
    }

    public virtual void Update(float deltaTime)
    {
        ++Frame;
        Time += deltaTime;
        DeltaTime = deltaTime;

        SubmitBatchedCommands();
        InvokeFrameStartListeners();

        foreach (var listener in GetListeners<IUpdateListener>()) {
            try {
                using (Profile("Update", listener)) {
                    listener.OnUpdate(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUpdateListener method for {listener}: " + e);
            }
        }

        InvokeInternalFrameListeners();
    }

    private void InvokeFrameStartListeners()
    {
        foreach (var listener in GetListeners<IFrameStartListener>()) {
            try {
                using (Profile("FrameStart", listener)) {
                    listener.OnFrameStart(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IFrameStartListener method for {listener}: " + e);
            }
        }
    }

    private void InvokeInternalFrameListeners()
    {
        foreach (var listener in GetListeners<IResourceUpdateListener>()) {
            try {
                using (Profile("ResourceUpdate", listener)) {
                    listener.OnResourceUpdate(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IResourceUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<IEngineUpdateListener>()) {
            try {
                using (Profile("EngineUpdate", listener)) {
                    listener.OnEngineUpdate(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IEngineUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<ILateUpdateListener>()) {
            try {
                using (Profile("LateUpdate", listener)) {
                    listener.OnLateUpdate(this);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILateUpdateListener method for {listener}: " + e);
            }
        }
    }

    public ReadOnlySpan<TListener> GetListeners<TListener>()
    {
        if (_listeners.TryGetValue(typeof(TListener), out var raw)) {
            return CollectionsMarshal.AsSpan((List<TListener>)raw);
        }

        var list = (List<TListener>)_listeners.AddOrUpdate(typeof(TListener),
            _ => GetSublayersRecursively<TListener>().ToList(),
            (_, list) => list);

        return CollectionsMarshal.AsSpan(list);
    }

    public virtual void SubmitBatchedCommands()
    {
        foreach (var target in _commandTargets.Values) {
            target.SubmitBatch();
        }
    }

    public void SendCommandBatched<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().AddBatchedCommand(command);

    public void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.Add(command);

    public bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.TryTake(out command);

    public ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.Take();

    public IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.GetConsumingEnumerable();

    private CommandTarget GetCommandTarget<TTarget>()
        => _commandTargets.GetOrAdd(typeof(TTarget), _ => new());

    public Profile? GetProfile(string path)
        => _profiles.TryGetValue(path, out var profile) ? profile : null;

    public Profile? GetProfile(string category, object target)
        => _profiles.TryGetValue(
                GetProfileKey(category, target), out var profile)
            ? profile : null;
    
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
        => _profileKeys.GetOrAdd((category, target), p => p.Item1 + '/' + p.Item2);
}