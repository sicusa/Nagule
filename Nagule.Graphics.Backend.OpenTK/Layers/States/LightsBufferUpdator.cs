namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class LightsBufferUpdator : Layer, ILoadListener, IEngineUpdateListener
{
    private record struct DirtyLightEntry(Guid Id, Vector3 Position, Vector3 Direction);

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
    {
        public readonly List<DirtyLightEntry> DirtyLights = new();

        public override Guid? Id => Guid.Empty;

        public unsafe override void Execute(ICommandContext context)
        {
            bool bufferGot = false;
            ref var buffer = ref Unsafe.NullRef<LightsBuffer>();
            LightParameters* pointer = null;
            
            var span = CollectionsMarshal.AsSpan(DirtyLights);
            foreach (ref var tuple in span) {
                if (!context.TryGet<LightData>(tuple.Id, out var data)) {
                    continue;
                }

                if (!bufferGot) {
                    bufferGot = true;
                    buffer = ref context.RequireAny<LightsBuffer>();
                    pointer = (LightParameters*)buffer.Pointer;
                }

                ref var pars = ref buffer.Parameters[data.Index];
                pars.Position = tuple.Position;
                pars.Direction = tuple.Direction;

                pointer[data.Index] = pars;
            }
        }

        public override void Merge(ICommand other)
        {
            if (other is not UpdateCommand converted) {
                return;
            }
            OrderedListHelper.Merge(DirtyLights, converted.DirtyLights,
                (in DirtyLightEntry e1, in DirtyLightEntry e2) => e1.Id.CompareTo(e2.Id));
        }

        public override void Dispose()
        {
            base.Dispose();
            DirtyLights.Clear();
        }
    }

    private Group<Resource<Light>> _lightGroup = new();
    [AllowNull] private IEnumerable<Guid> _dirtyLightIds;

    public void OnLoad(IContext context)
    {
        _dirtyLightIds = QueryUtil.Intersect(_lightGroup, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context)
    {
        _lightGroup.Query(context);

        if (_dirtyLightIds.Any()) {
            var cmd = UpdateCommand.Create();

            foreach (var id in _dirtyLightIds) {
                ref readonly var transform = ref context.Inspect<Transform>(id);
                cmd.DirtyLights.Add(new(id, transform.Position, transform.Forward));
            }

            context.SendCommandBatched(cmd);
        }
    }
}