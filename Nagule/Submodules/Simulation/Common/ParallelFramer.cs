namespace Nagule;

using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Sia;

using TaskFunc = Func<ParallelFramer, object?, bool>;
using DelayQueue = Queue<ParallelFramer.TaskEntry>;

public abstract class ParallelFramer : FramerBase
{
    public record struct TaskEntry(object? Argument, TaskFunc Task);

    public event Action<TaskEntry>? OnTaskExecuted;

    protected ILogger Logger { get; private set; } = null!;

    private readonly ThreadLocal<SwappingQueue<(EntityRef?, TaskEntry)>> _queue =
        new(() => new(), trackAllValues: true);

    private readonly LinkedList<TaskEntry> _globalDelayedTasks = new();
    private readonly List<(TaskEntry, LinkedListNode<TaskEntry>)> _globalDelayedTasksList = [];
    private bool _globalDelayedTasksDirty = false;

    private readonly Dictionary<EntityRef, DelayQueue> _delayQueues = [];
    private readonly List<EntityRef> _delayQueuesToRemove = [];
    private readonly Stack<DelayQueue> _delayQueuePool = new();

    private static readonly TaskFunc s_terminateTask = (_, _) => false;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        Logger = CreateLogger(world, world.AcquireAddon<LogLibrary>());
    }

    protected abstract ILogger CreateLogger(World world, LogLibrary logLib);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DelayQueue CreateDeleyQueue()
        => _delayQueuePool.TryPop(out var pooled) ? pooled : new();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReleaseDeleyQueue(DelayQueue queue)
        => _delayQueuePool.Push(queue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start<TArg>(TArg argument, Func<ParallelFramer, TArg, bool> action)
        where TArg : class
        => _queue.Value!.Add((null, new(argument, Unsafe.As<TaskFunc>(action))));

    public void Start(Func<bool> action)
        => Start(action, (framer, action) =>
            Unsafe.As<Func<bool>>(action)!.Invoke());

    public void Start(Action action)
        => Start(action, (framer, action) => {
            Unsafe.As<Action>(action)!.Invoke();
            return true;
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(in EntityRef entity, object argument, TaskFunc action)
        => _queue.Value!.Add((entity, new(argument, action)));

    public void Enqueue<TArg>(in EntityRef entity, TArg argument, Func<TArg, bool> action)
        where TArg : class
        => Enqueue(entity, argument, Unsafe.As<TaskFunc>(action));

    public void Enqueue(in EntityRef entity, Func<bool> action)
    {
        _queue.Value!.Add((entity, new(action,
            static (framer, action) => Unsafe.As<Func<bool>>(action)!.Invoke())));
    }

    public void Enqueue(in EntityRef entity, Action action)
    {
        _queue.Value!.Add((entity, new(action,
            static (framer, action) => {
                Unsafe.As<Action>(action)!.Invoke();
                return true;
            })));
    }

    public void Terminate(in EntityRef entity)
        => _queue.Value!.Add((entity, new(null!, s_terminateTask)));

    protected override void OnTick()
    {
        foreach (var (entity, queue) in _delayQueues) {
            while (queue.TryPeek(out var entry)) {
                if (!RunTaskSafely(entry)) { break; }
                queue.Dequeue();
            }
            if (queue.Count == 0) {
                _delayQueuesToRemove.Add(entity);
            }
        }

        if (_delayQueuesToRemove.Count != 0) {
            foreach (var queueEntity in _delayQueuesToRemove) {
                _delayQueues.Remove(queueEntity, out var queue);
                ReleaseDeleyQueue(queue!);
            }
            _delayQueuesToRemove.Clear();
        }

        foreach (var seq in _queue.Values) {
            var queue = seq.Swap();
            foreach (var (entityRaw, entry) in queue.AsSpan()) {
                if (entityRaw is not EntityRef entity) {
                    if (!RunTaskSafely(entry)) {
                        _globalDelayedTasks.AddLast(entry);
                        _globalDelayedTasksDirty = true;
                    }
                    continue;
                }
                if (entry.Task == s_terminateTask) {
                    _delayQueues.Remove(entity);
                    continue;
                }
                if (_delayQueues.TryGetValue(entity, out var delayQueue)) {
                    delayQueue.Enqueue(entry);
                    continue;
                }
                if (!RunTaskSafely(entry)) {
                    delayQueue = CreateDeleyQueue();
                    delayQueue.Enqueue(entry);
                    _delayQueues.Add(entity, delayQueue);
                }
            }
            queue.Clear();
        }

        foreach (var (entry, node) in _globalDelayedTasksList.AsSpan()) {
            if (RunTaskSafely(entry)) {
                _globalDelayedTasks.Remove(node);
                _globalDelayedTasksDirty = true;
            }
        }

        if (_globalDelayedTasksDirty) {
            _globalDelayedTasksDirty = false;
            _globalDelayedTasksList.Clear();

            var node = _globalDelayedTasks.First;
            while (node != null) {
                ref var taskEntry = ref node.ValueRef;
                _globalDelayedTasksList.Add((taskEntry, node));
                node = node.Next;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RunTaskSafely(in TaskEntry entry)
    {
        try {
            return entry.Task(this, entry.Argument);
        }
        catch (Exception e) {
            Logger.LogError("Unhandled exception: {Exception}", e);
            return true;
        }
        finally {
            OnTaskExecuted?.Invoke(entry);
        }
    }
}