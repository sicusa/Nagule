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
    private Group<Resource<Light>> _lightGroup = new();
    [AllowNull] private IEnumerable<Guid> _dirtyLightIds;
    private ConcurrentQueue<(Guid[], int)> _dirtyLightQueue = new();

    public void OnLoad(IContext context)
    {
        _dirtyLightIds = QueryUtil.Intersect(_lightGroup, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context)
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

    public unsafe void OnRender(IContext context)
    {
        while (_dirtyLightQueue.TryDequeue(out var tuple)) {
            var (ids, length) = tuple;

            bool bufferGot = false;
            ref var buffer = ref Unsafe.NullRef<LightsBuffer>();
            LightParameters* pointer = null;

            try {
                for (int i = 0; i != length; ++i) {
                    var id = ids[i];
                    if (!context.TryGet<LightData>(id, out var data)) {
                        continue;
                    }

                    if (!bufferGot) {
                        bufferGot = true;
                        buffer = ref context.RequireAny<LightsBuffer>();
                        pointer = (LightParameters*)buffer.Pointer;
                    }

                    ref readonly var transform = ref context.Inspect<Transform>(id);
                    ref var pars = ref buffer.Parameters[data.Index];
                    pars.Position = transform.Position;
                    pars.Direction = transform.Forward;

                    pointer[data.Index] = pars;
                }
            }
            finally {
                ArrayPool<Guid>.Shared.Return(ids);
            }
        }
    }
}