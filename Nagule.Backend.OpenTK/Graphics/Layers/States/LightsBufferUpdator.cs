namespace Nagule.Backend.OpenTK.Graphics;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class LightsBufferUpdator : VirtualLayer, ILoadListener, IEngineUpdateListener
{
    private Group<Light> _g = new();
    [AllowNull] private IEnumerable<Guid> _dirtyLightIds;

    public void OnLoad(IContext context)
    {
        _dirtyLightIds = QueryUtil.Intersect(_g, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context, float deltaTime)
    {
        bool bufferGot = false;
        ref var buffer = ref Unsafe.NullRef<LightsBuffer>();

        int minIndex = 0;
        int maxIndex = 0;

        _g.Query(context);

        foreach (var id in _dirtyLightIds) {
            ref var data = ref context.Require<LightData>(id);

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

        if (bufferGot) {
            var src = new Span<LightParameters>(buffer.Parameters, minIndex, maxIndex - minIndex + 1);
            var dst = new Span<LightParameters>((LightParameters*)buffer.Pointer + minIndex, buffer.Capacity);
            src.CopyTo(dst);
        }
    }
}