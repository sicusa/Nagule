namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using CommunityToolkit.HighPerformance;
using Sia;

public record struct Mesh3DInstanceGroupKey(EntityRef MaterialState, Mesh3DData MeshData);

public sealed class Mesh3DInstanceGroup : IDisposable
{
    public const int InitialCapacity = 1;

    public IReadOnlyList<EntityRef> Entities => _entities;

    public int Capacity { get; private set; }
    public int Count { get; private set; }
    public int CulledCount {
        get {
            if (_culledCount == -1) {
                GL.GetQueryObjecti(CulledQueryHandle.Handle,
                    QueryObjectParameterName.QueryResult, ref _culledCount);
            }
            return _culledCount;
        }
    }

    public AABB BoundingBox { get; }

    public Mesh3DInstanceGroupKey Key { get; }

    public VertexArrayHandle VertexArrayHandle { get; }
    public VertexArrayHandle CullingVertexArrayHandle { get; }
    public VertexArrayHandle CulledVertexArrayHandle { get; }

    public QueryHandle CulledQueryHandle { get; }

    public BufferHandle InstanceBuffer { get; private set; }
    public IntPtr Pointer { get; private set; }

    public BufferHandle CulledInstanceBuffer { get; }

    public unsafe ref Matrix4x4 this[int index] =>
        ref ((Matrix4x4*)Pointer)[index];
    
    private int _culledCount = -1;
    private uint _vertexAttrStartIndex;
    private readonly List<EntityRef> _entities = [];

    private const int Matrix4x4Length = 16 * 4;

    public Mesh3DInstanceGroup(Mesh3DInstanceGroupKey key, Mesh3DDataBuffer meshDataState)
    {
        Key = key;

        Capacity = InitialCapacity;
        Count = 0;

        BoundingBox = meshDataState.BoundingBox;

        VertexArrayHandle = new(GL.GenVertexArray());
        CullingVertexArrayHandle = new(GL.GenVertexArray());
        CulledVertexArrayHandle = new(GL.GenVertexArray());
        CulledQueryHandle = new(GL.GenQuery());

        InstanceBuffer = new(GL.GenBuffer());
        CulledInstanceBuffer = new(GL.GenBuffer());

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        Pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * Matrix4x4Length);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        InitializeVertexArrays(meshDataState);
        BindInstanceBuffers();
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

    public int Add(EntityRef entity, in Matrix4x4 instance)
    {
        var index = Count;
        Count++;
        EnsureCapacity(Count);
        this[index] = instance;
        _entities.Add(entity);
        return index;
    }

    public void Remove(int index)
    {
        Count--;
        this[index] = this[Count];

        _entities[index] = _entities[Count];
        _entities.RemoveAt(Count);
    }

    private void EnsureCapacity(int capacity)
    {
        int prevCapacity = Capacity;
        if (prevCapacity >= capacity) { return; }

        int newCapacity = Math.Max(prevCapacity * 2, 6);
        while (newCapacity < capacity) { newCapacity *= 2; }
        Capacity = newCapacity;

        var newBuffer = GL.GenBuffer();

        GL.DeleteBuffer(InstanceBuffer.Handle);
        InstanceBuffer = new(newBuffer);

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, newBuffer);
        Pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * Matrix4x4Length);

        int i = 0;
        foreach (var entity in _entities.AsSpan()) {
            this[i] = entity.GetFeatureNode<Transform3D>().World;
            i++;
        }

        BindInstanceBuffers();
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(VertexArrayHandle.Handle);
        GL.DeleteVertexArray(CullingVertexArrayHandle.Handle);
        GL.DeleteVertexArray(CulledVertexArrayHandle.Handle);

        GL.DeleteBuffer(InstanceBuffer.Handle);
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

    private void BindInstanceBuffers()
    {
        GL.BindVertexArray(VertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        EnableMatrix4x4Attributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(CullingVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        EnableMatrix4x4Attributes(4);

        GL.BindVertexArray(CulledVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, CulledInstanceBuffer.Handle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, Capacity * Matrix4x4Length, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        EnableMatrix4x4Attributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(0);
    }

    private static void EnableMatrix4x4Attributes(uint startIndex, uint divisor = 0)
    {
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Matrix4x4Length, 0);
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Matrix4x4Length, 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Matrix4x4Length, 2 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Matrix4x4Length, 3 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);
    }
}

public class GLMesh3DInstanceLibrary : IAddon
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
