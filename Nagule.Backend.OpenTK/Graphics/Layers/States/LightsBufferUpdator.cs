namespace Nagule.Backend.OpenTK.Graphics;

using System.Runtime.CompilerServices;

using Aeco;

using Nagule.Graphics;

public class LightsBufferUpdator : VirtualLayer, IUpdateListener
{
    private Query<Light, LightData> _q = new();

    public unsafe void OnUpdate(IContext context, float deltaTime)
    {
        HashSet<Guid>? dirtyIds = null;
        bool bufferGot = false;
        ref var buffer = ref Unsafe.NullRef<LightsBuffer>();

        int minIndex = 0;
        int maxIndex = 0;

        foreach (var id in _q.Query(context)) {
            dirtyIds ??= context.AcquireAny<DirtyTransforms>().Ids;
            if (!dirtyIds.Contains(id)) {
                continue;
            }

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