namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Sia;

using TaskFunc = Func<object, bool>;
using DelayQueue = Queue<(object, Func<object, bool>)>;

public abstract class ParallelFrame : Frame
{
    public event Action<TaskFunc, object>? OnTaskExecuted;

    [AllowNull] protected ILogger Logger { get; private set; }

    private readonly List<(EntityRef?, object, TaskFunc)> _tasks1 = [];
    private readonly List<(EntityRef?, object, TaskFunc)> _tasks2 = [];
    private int _swapTag;

    private readonly LinkedList<(object, TaskFunc)> _globalDelayedTasks = new();
    private readonly List<(object, TaskFunc, LinkedListNode<(object, TaskFunc)>)> _globalDelayedTasksList = [];
    private bool _globalDelayedTasksDirty = false;

    private readonly Dictionary<EntityRef, DelayQueue> _delayQueues = [];
    private readonly List<EntityRef> _delayQueuesToRemove = [];

    private readonly Stack<DelayQueue> _delayQueuePool = new();

    private static readonly TaskFunc s_terminateTask = _ => false;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        Logger = CreateLogger(world, world.GetAddon<LogLibrary>());
    }

    protected abstract ILogger CreateLogger(World world, LogLibrary logLib);

    private DelayQueue CreateDeleyQueue()
        => _delayQueuePool.TryPop(out var pooled) ? pooled : new();
    
    private void ReleaseDeleyQueue(DelayQueue queue)
        => _delayQueuePool.Push(queue);

    public void Start<TArg>(TArg argument, Func<TArg, bool> action)
        where TArg : class
        => Start(argument, Unsafe.As<TaskFunc>(action));

    public void Start(object argument, TaskFunc action)
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        pendingTasks.Add((null, argument, action));
    }

    public void Start(Func<bool> action)
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        pendingTasks.Add((null, action, 
            static action => Unsafe.As<Func<bool>>(action).Invoke()));
    }

    public void Enqueue<TArg>(in EntityRef entity, TArg argument, Func<TArg, bool> action)
        where TArg : class
        => Enqueue(entity, argument, Unsafe.As<TaskFunc>(action));

    public void Enqueue(in EntityRef entity, object argument, TaskFunc action)
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        pendingTasks.Add((entity, argument, action));
    }

    public void Enqueue(in EntityRef entity, Func<bool> action)
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        pendingTasks.Add((entity, action,
            static action => Unsafe.As<Func<bool>>(action).Invoke()));
    }

    public void Terminate(in EntityRef entity)
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        pendingTasks.Add((entity, null!, s_terminateTask));
    }

    protected override void OnTick()
    {
        var pendingTasks = _swapTag == 0 ? _tasks1 : _tasks2;
        MathUtils.InterlockedXor(ref _swapTag, 1);

        foreach (var (argument, task, node) in _globalDelayedTasksList.AsSpan()) {
            if (task(argument)) {
                _globalDelayedTasks.Remove(node);
                _globalDelayedTasksDirty = true;
            }
        }

        if (_globalDelayedTasksDirty) {
            _globalDelayedTasksDirty = false;
            _globalDelayedTasksList.Clear();

            var node = _globalDelayedTasks.First;
            while (node != null) {
                ref var value = ref node.ValueRef;
                _globalDelayedTasksList.Add((value.Item1, value.Item2, node));
                node = node.Next;
            }
        }

        foreach (var (entity, queue) in _delayQueues) {
            while (queue.TryPeek(out var task)) {
                if (!RunTaskSafely(task.Item2, task.Item1)) { break; }
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

        foreach (var (entityRaw, argument, task) in pendingTasks.AsSpan()) {
            if (entityRaw is not EntityRef entity) {
                if (!RunTaskSafely(task, argument)) {
                    var node = _globalDelayedTasks.AddLast((argument, task));
                    _globalDelayedTasksList.Add((argument, task, node));
                }
                continue;
            }
            if (task == s_terminateTask) {
                _delayQueues.Remove(entity);
                continue;
            }
            if (_delayQueues.TryGetValue(entity, out var delayQueue)) {
                delayQueue.Enqueue((argument, task));
                continue;
            }
            if (!RunTaskSafely(task, argument)) {
                delayQueue = CreateDeleyQueue();
                delayQueue.Enqueue((argument, task));
                _delayQueues.Add(entity, delayQueue);
            }
        }

        pendingTasks.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RunTaskSafely(TaskFunc task, object argument)
    {
        try {
            return task(argument);
        }
        catch (Exception e) {
            Logger.LogError("Unhandled exception: {Exception}", e);
            return true;
        }
        finally {
            OnTaskExecuted?.Invoke(task, argument);
        }
    }
}