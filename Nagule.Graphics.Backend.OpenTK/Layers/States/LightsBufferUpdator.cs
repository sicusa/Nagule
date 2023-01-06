namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class LightsBufferUpdator : VirtualLayer, ILoadListener, IEngineUpdateListener
{
    private class UpdateCommand : Command<UpdateCommand>
    {
        public readonly List<Guid> DirtyLightIds = new();

        public unsafe override void Execute(IContext context)
        {
            bool bufferGot = false;
            ref var buffer = ref Unsafe.NullRef<LightsBuffer>();
            LightParameters* pointer = null;
            
            foreach (var id in DirtyLightIds) {
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

        public override void Dispose()
        {
            base.Dispose();
            DirtyLightIds.Clear();
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
            cmd.DirtyLightIds.AddRange(_dirtyLightIds);
            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }
}