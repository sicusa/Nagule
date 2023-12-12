namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Sia;

public class Mesh3DManager : GraphicsAssetManagerBase<Mesh3D, Mesh3DAsset, Mesh3DState>
{
    public IReadOnlyDictionary<Mesh3DData, Mesh3DDataState> DataStates => _dataStates;

    private readonly Dictionary<Mesh3DData, Mesh3DDataState> _dataStates = [];
    private readonly Dictionary<object, Mesh3DBufferState> _bufferStates = [];

    [AllowNull] private MaterialManager _materialManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _materialManager = world.GetAddon<MaterialManager>();

        Listen((EntityRef entity, ref Mesh3D snapshot, in Mesh3D.SetData cmd) => {
            var data = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                UnreferDataState(state.DataEntry!);
                state.DataEntry = AcquireDataState(data);
                return true;
            });
        });

        Listen((EntityRef entity, in Mesh3D.SetMaterial cmd) => {
            var material = cmd.Value;
            var matEntity = _materialManager.Acquire(material, entity);
            entity.UnreferAsset(entity.Get<AssetMetadata>().FindReferred<Material>()!.Value);
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                state.MaterialEntity = matEntity;
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Mesh3D asset)
    {
        var data = asset.Data;
        var matEntity = _materialManager.Acquire(asset.Material, entity);

        RenderFrame.Enqueue(entity, () => {
            var state = new Mesh3DState {
                MaterialEntity = matEntity,
                DataEntry = AcquireDataState(data),
            };
            RenderStates.Set(entity, state);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Mesh3D asset)
    {
        RenderFrame.Enqueue(entity, () => {
            if (RenderStates.Remove(entity, out var state)) {
                UnreferDataState(state.DataEntry!);
            }
            return true;
        });
    }

    private Mesh3DDataState AcquireDataState(Mesh3DData data)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_dataStates, data, out bool exists);
        if (exists) {
            Interlocked.Increment(ref state!.RefCount);
            return state;
        }

        var handle = GL.GenBuffer();
        state = new Mesh3DDataState(data, new(handle));

        var indices = data.Indices;
        var vertices = data.Vertices;
        var texCoords = data.TexCoords;
        var normals = data.Normals;
        var tangents = data.Tangents;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 2 * 16, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, handle);

        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 12, state.BoundingBox.Min);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 16, 12, state.BoundingBox.Max);
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        var buffers = state.BufferEntries;
        if (vertices.Length != 0) { buffers[Mesh3DBufferType.Vertices] = AcquireBufferState(vertices); }
        if (texCoords.Length != 0) { buffers[Mesh3DBufferType.TexCoords] = AcquireBufferState(texCoords); }
        if (normals.Length != 0) { buffers[Mesh3DBufferType.Normals] = AcquireBufferState(normals); }
        if (tangents.Length != 0) { buffers[Mesh3DBufferType.Tangents] = AcquireBufferState(tangents); }
        if (indices.Length != 0) { buffers[Mesh3DBufferType.Indices] = AcquireBufferState(indices); }

        return state;
    }

    private void UnreferDataState(Mesh3DDataState state)
    {
        if (Interlocked.Decrement(ref state.RefCount) != 0) {
            return;
        }
        GL.DeleteBuffer(state.UniformBufferHandle.Handle);
        foreach (var bufferEntry in state.BufferEntries.Values) {
            if (Interlocked.Decrement(ref state.RefCount) == 0) {
                GL.DeleteBuffer(bufferEntry.Handle.Handle);
                _bufferStates.Remove(bufferEntry.Key);
            }
        }
        _dataStates.Remove(state.Key);
    }

    private Mesh3DBufferState AcquireBufferState(ImmutableArray<Vector3> array)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_bufferStates, array, out bool exists);
        if (exists) {
            Interlocked.Increment(ref state!.RefCount);
            return state;
        }

        var handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, handle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, array.AsSpan(), BufferUsageARB.StaticDraw);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        state = new(array, new(handle));
        return state;
    }

    private Mesh3DBufferState AcquireBufferState(ImmutableArray<uint> array)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_bufferStates, array, out bool exists);
        if (exists) {
            Interlocked.Increment(ref state!.RefCount);
            return state;
        }

        var handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, handle);
        GL.BufferData(BufferTargetARB.ElementArrayBuffer, array.AsSpan(), BufferUsageARB.StaticDraw);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        state = new(array, new(handle));
        return state;
    }
}