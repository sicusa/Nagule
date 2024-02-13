namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using Sia;

public record struct Mesh3DInstanceGroupKey(EntityRef MaterialState, Mesh3DData MeshData);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Mesh3DInstance
{
    public Matrix4x4 ObjectToWorld;
    public int LayerMask;
}

public sealed class Mesh3DInstanceGroup : IDisposable
{
    public const int InitialCapacity = 1;

    public IReadOnlyList<EntityRef> Entities => _entities;

    public int Count => _entities.Count;

    public int CulledCount {
        get {
            if (_culledCount == -1) {
                GL.GetQueryObjecti(CulledQueryHandle.Handle,
                    QueryObjectParameterName.QueryResult, ref _culledCount);
            }
            return _culledCount;
        }
    }

    public Mesh3DInstanceGroupKey Key { get; }
    public AABB BoundingBox { get; }
    public GLArrayBuffer<Mesh3DInstance> InstanceBuffer { get; }

    public VertexArrayHandle VertexArrayHandle { get; }
    public VertexArrayHandle CullingVertexArrayHandle { get; }
    public VertexArrayHandle CulledVertexArrayHandle { get; }
    public QueryHandle CulledQueryHandle { get; }
    public BufferHandle CulledInstanceBuffer { get; }
    
    private int _culledCount = -1;
    private uint _vertexAttrStartIndex;
    private readonly List<EntityRef> _entities = [];

    public Mesh3DInstanceGroup(Mesh3DInstanceGroupKey key, Mesh3DDataBuffer meshDataState)
    {
        Key = key;
        BoundingBox = meshDataState.BoundingBox;
        InstanceBuffer = new(InitialCapacity);

        VertexArrayHandle = new(GL.GenVertexArray());
        CullingVertexArrayHandle = new(GL.GenVertexArray());
        CulledVertexArrayHandle = new(GL.GenVertexArray());
        CulledQueryHandle = new(GL.GenQuery());
        CulledInstanceBuffer = new(GL.GenBuffer());

        InitializeVertexArrays(meshDataState);
        BindBufferAttributes();
    }

    public void Cull()
    {
        GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, CulledInstanceBuffer.Handle);
        GL.BindVertexArray(CullingVertexArrayHandle.Handle);

        GL.BeginQuery(QueryTarget.PrimitivesGenerated, CulledQueryHandle.Handle);
        GL.BeginTransformFeedback(GLPrimitiveType.Points);
        GL.DrawArrays(GLPrimitiveType.Points, 0, Count);
        GL.EndTransformFeedback();
        GL.EndQuery(QueryTarget.PrimitivesGenerated);

        _culledCount = -1;
    }

    public int Add(EntityRef entity)
    {
        var prevCount = Count;
        _entities.Add(entity);
        InstanceBuffer.EnsureCapacity(prevCount + 1, out bool modified, prevCount);
        if (modified) {
            BindBufferAttributes();
        }
        return prevCount;
    }

    public void Remove(int index)
    {
        var lastIndex = Count - 1;
        _entities[index] = _entities[lastIndex];
        _entities.RemoveAt(lastIndex);
        InstanceBuffer[index] = InstanceBuffer[lastIndex];
    }

    public void Dispose()
    {
        InstanceBuffer.Dispose();

        GL.DeleteVertexArray(VertexArrayHandle.Handle);
        GL.DeleteVertexArray(CullingVertexArrayHandle.Handle);
        GL.DeleteVertexArray(CulledVertexArrayHandle.Handle);
        GL.DeleteBuffer(CulledInstanceBuffer.Handle);
        GL.DeleteQuery(CulledQueryHandle.Handle);
    }

    private void InitializeVertexArrays(Mesh3DDataBuffer meshData)
    {
        GL.BindVertexArray(VertexArrayHandle.Handle);
        _vertexAttrStartIndex = meshData.EnableVertexAttribArrays();

        GL.BindVertexArray(CulledVertexArrayHandle.Handle);
        meshData.EnableVertexAttribArrays();

        GL.BindVertexArray(0);
    }

    private void BindBufferAttributes()
    {
        GL.BindVertexArray(VertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle.Handle);
        EnableInstanceAttributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(CullingVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle.Handle);
        EnableInstanceAttributes(4);

        GL.BindVertexArray(CulledVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, CulledInstanceBuffer.Handle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, InstanceBuffer.Capacity * InstanceBuffer.ElementSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        EnableInstanceAttributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(0);
    }

    private void EnableInstanceAttributes(uint startIndex, uint divisor = 0)
    {
        // matrix
        
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, InstanceBuffer.ElementSize, 0);
        GL.VertexAttribDivisor(startIndex, divisor);

        startIndex++;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, InstanceBuffer.ElementSize, 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        startIndex++;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, InstanceBuffer.ElementSize, 2 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        startIndex++;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, InstanceBuffer.ElementSize, 3 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        // layer mask

        startIndex++;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Int, false, InstanceBuffer.ElementSize, 4 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);
    }
}

public class Mesh3DInstanceLibrary : IAddon
{
    public Dictionary<Mesh3DInstanceGroupKey, Mesh3DInstanceGroup> Groups { get; } = [];
    public Dictionary<EntityRef, (Mesh3DInstanceGroup Group, int Index)> InstanceEntries { get; } = [];

    public void OnUninitialize(World world)
    {
        world.GetAddon<RenderFramer>().Start(() => {
            foreach (var group in Groups.Values) {
                group.Dispose();
            }
            Groups.Clear();
            InstanceEntries.Clear();
            return true;
        });
    }
}
