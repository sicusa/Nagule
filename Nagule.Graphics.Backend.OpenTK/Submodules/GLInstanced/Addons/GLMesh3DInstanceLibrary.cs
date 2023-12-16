namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Sia;

[StructLayout(LayoutKind.Sequential)]
public struct Mesh3DInstance(Matrix4x4 objectToWorld)
{
    public static readonly int MemorySize = Unsafe.SizeOf<Mesh3DInstance>();
    public Matrix4x4 ObjectToWorld = objectToWorld;
}

public record struct Mesh3DInstanceGroupKey(EntityRef MaterialEntity, Mesh3DData MeshData);

public sealed class Mesh3DInstanceGroup : IDisposable
{
    public const int InitialCapacity = 1;

    public IReadOnlyList<EntityRef> Entities => _entities;

    public int Capacity { get; private set; }
    public int Count { get; private set; }

    public Mesh3DInstanceGroupKey Key { get; }

    public VertexArrayHandle VertexArrayHandle { get; }
    public VertexArrayHandle PreCullingVertexArrayHandle { get; }
    public VertexArrayHandle CulledVertexArrayHandle { get; }
    public VertexArrayHandle CullingVertexArrayHandle { get; }

    public QueryHandle CulledQueryHandle { get; }

    public BufferHandle InstanceBuffer { get; private set; }
    public IntPtr Pointer { get; private set; }

    public BufferHandle PreCulledInstanceBuffer { get; }
    public BufferHandle CulledInstanceBuffer { get; }

    public unsafe ref Mesh3DInstance this[int index] =>
        ref ((Mesh3DInstance*)Pointer)[index];
    
    private uint _vertexAttrStartIndex;
    private readonly List<EntityRef> _entities = [];

    public Mesh3DInstanceGroup(Mesh3DInstanceGroupKey key, Mesh3DDataState meshDataState)
    {
        Key = key;

        Capacity = InitialCapacity;
        Count = 0;

        VertexArrayHandle = new(GL.GenVertexArray());
        PreCullingVertexArrayHandle = new(GL.GenVertexArray());
        CullingVertexArrayHandle = new(GL.GenVertexArray());
        CulledVertexArrayHandle = new(GL.GenVertexArray());

        InstanceBuffer = new(GL.GenBuffer());
        PreCulledInstanceBuffer = new(GL.GenBuffer());
        CulledInstanceBuffer = new(GL.GenBuffer());

        CulledQueryHandle = new(GL.GenQuery());

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        Pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * Mesh3DInstance.MemorySize);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        InitializeVertexArrays(meshDataState);
        BindInstanceBuffers();
    }

    public int Add(EntityRef entity, in Mesh3DInstance instance)
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
        Pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * Mesh3DInstance.MemorySize);

        int i = 0;
        foreach (var entity in _entities.AsSpan()) {
            this[i] = new(entity.Get<Feature>().Node.Get<Transform3D>().World);
            i++;
        }

        BindInstanceBuffers();
    }

    public void Dispose()
    {
        GL.DeleteVertexArray(VertexArrayHandle.Handle);
        GL.DeleteVertexArray(PreCullingVertexArrayHandle.Handle);
        GL.DeleteVertexArray(CullingVertexArrayHandle.Handle);
        GL.DeleteVertexArray(CulledVertexArrayHandle.Handle);

        GL.DeleteBuffer(InstanceBuffer.Handle);
        GL.DeleteBuffer(PreCulledInstanceBuffer.Handle);
        GL.DeleteBuffer(CulledInstanceBuffer.Handle);

        GL.DeleteQuery(CulledQueryHandle.Handle);
    }

    private void InitializeVertexArrays(Mesh3DDataState meshDataState)
    {
        GL.BindVertexArray(VertexArrayHandle.Handle);
        _vertexAttrStartIndex = meshDataState.EnableVertexAttribArrays();

        GL.BindVertexArray(CulledVertexArrayHandle.Handle);
        meshDataState.EnableVertexAttribArrays();

        GL.BindVertexArray(0);
    }

    private void BindInstanceBuffers()
    {
        GL.BindVertexArray(VertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        EnableMatrix4x4Attributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(PreCullingVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, InstanceBuffer.Handle);
        EnableMatrix4x4Attributes(4);

        GL.BindVertexArray(CullingVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, PreCulledInstanceBuffer.Handle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, Capacity * Mesh3DInstance.MemorySize, IntPtr.Zero, BufferUsageARB.StreamCopy);
        EnableMatrix4x4Attributes(4);

        GL.BindVertexArray(CulledVertexArrayHandle.Handle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, CulledInstanceBuffer.Handle);
        GL.BufferData(BufferTargetARB.ArrayBuffer, Capacity * Mesh3DInstance.MemorySize, IntPtr.Zero, BufferUsageARB.StreamCopy);
        EnableMatrix4x4Attributes(_vertexAttrStartIndex, 1);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }

    private static void EnableMatrix4x4Attributes(uint startIndex, uint divisor = 0)
    {
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Mesh3DInstance.MemorySize, 0);
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Mesh3DInstance.MemorySize, 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Mesh3DInstance.MemorySize, 2 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, Mesh3DInstance.MemorySize, 3 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);
    }
}

public class GLMesh3DInstanceLibrary : IAddon
{
    public Dictionary<Mesh3DInstanceGroupKey, Mesh3DInstanceGroup> Groups { get; } = [];
    public Dictionary<EntityRef, (Mesh3DInstanceGroup Group, int Index)> InstanceEntries { get; } = [];

    public void OnUninitialize(World world)
    {
        world.GetAddon<RenderFrame>().Start(() => {
            foreach (var group in Groups.Values) {
                group.Dispose();
            }
            Groups.Clear();
            InstanceEntries.Clear();
            return true;
        });
    }
}
