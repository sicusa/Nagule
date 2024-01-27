namespace Nagule.Graphics.Backends.OpenTK;

using CommunityToolkit.HighPerformance.Buffers;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Sia;

public class Light3DUpdator : RendererBase
{
    public record struct Entry(EntityRef StateEntity, Vector3 Position, Vector3 Direction);

    internal Dictionary<EntityRef, (MemoryOwner<Entry>, int)> PendingDict { get; } = [];

    [AllowNull] private Light3DLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Light3DLibrary>();
    }

    protected override void OnRender()
    {
        foreach (var (e, (mem, index)) in PendingDict) {
            ref var state = ref e.Get<Light3DState>();
            if (state.Type == LightType.None) {
                continue;
            }
            ref var pars = ref _lib.Parameters[state.Index];
            ref var buffer = ref _lib.GetBufferData(state.Index);
            ref var entry = ref mem.Span[index];
            pars.Position = buffer.Position = entry.Position;
            pars.Direction = buffer.Direction = entry.Direction;
        }
        PendingDict.Clear();
    }
}