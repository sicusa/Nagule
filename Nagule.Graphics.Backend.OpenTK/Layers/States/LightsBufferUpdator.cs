namespace Nagule.Graphics.Backend.OpenTK;

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class LightsBufferUpdator : VirtualLayer, ILoadListener, IEngineUpdateListener, IRenderListener
{
    private Group<Light> _lightGroup = new();
    [AllowNull] private IEnumerable<Guid> _dirtyLightIds;
    private ConcurrentQueue<(Guid[], int)> _dirtyLightQueue = new();

    public void OnLoad(IContext context)
    {
        _dirtyLightIds = QueryUtil.Intersect(_lightGroup, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context, float deltaTime)
    {
        _lightGroup.Query(context);
        if (_dirtyLightIds.Any()) {
            var ids = ArrayPool<Guid>.Shared.Rent(_lightGroup.Count);
            int i = 0;
            foreach (var id in _dirtyLightIds) {
                ids[i++] = id;
            }
            _dirtyLightQueue.Enqueue((ids, i));
        }
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_dirtyLightQueue.TryDequeue(out var tuple)) {
            var (ids, length) = tuple;

            bool bufferGot = false;
            ref var buffer = ref Unsafe.NullRef<LightsBuffer>();

            int minIndex = 0;
            int maxIndex = 0;

            try {
                for (int i = 0; i != length; ++i) {
                    var id = ids[i];
                    if (!context.TryGet<LightData>(id, out var data)) {
                        continue;
                    }

                    if (!bufferGot) {
                        bufferGot = true;
                        buffer = ref context.RequireAny<LightsBuffer>();

                        minIndex = data.Index;
                        maxIndex = data.Index;
                    }
                    else {
                        minIndex = Math.Min(minIndex, data.Index);
                        maxIndex = Math.Max(maxIndex, data.Index);
                    }

                    ref readonly var transform = ref context.Inspect<Transform>(id);
                    ref var pars = ref buffer.Parameters[data.Index];

                    pars.Position = transform.Position;
                    pars.Direction = transform.Forward;
                }
            }
            finally {
                ArrayPool<Guid>.Shared.Return(ids);
            }

            if (bufferGot) {
                var src = new Span<LightParameters>(buffer.Parameters, minIndex, maxIndex - minIndex + 1);
                var dst = new Span<LightParameters>((LightParameters*)buffer.Pointer + minIndex, buffer.Capacity);
                src.CopyTo(dst);
            }
        }
    }
}