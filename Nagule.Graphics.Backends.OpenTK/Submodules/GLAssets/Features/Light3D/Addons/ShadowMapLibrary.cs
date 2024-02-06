namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public record struct ShadowMapHandle(int Value);

public unsafe class ShadowMapLibrary : ViewBase
{
    public const int MaximumSamplerCount = 127;
    public const int SamplerSlotCount = MaximumSamplerCount + 1;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Sampler
    {
        public int Index;
        public float Strength;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SamplerSlot
    {
        public static readonly int MemorySize = Unsafe.SizeOf<SamplerSlot>();

        public int LightIndex;
        public int NextSlotIndex;
        public Sampler Sampler;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UniformHeader
    {
        public static readonly int MemorySize = Unsafe.SizeOf<UniformHeader>();

        public int ShadowMapWidth;
        public int ShadowMapHeight;

        public int PrimaryLightShadowMapIndex;
        public int SecondaryLightShadowMapIndex;

        public Matrix4x4 PrimaryLightMatrix;
        public Matrix4x4 SecondaryLightMatrix;
    }

    public event Action? OnTilesetRecreated;

    public int Resolution {
        get => _resolution;
        set {
            _resolution = value;
            UpdateShadowMapTileset();
            Header.ShadowMapWidth = value;
            Header.ShadowMapHeight = value;
        }
    }

    public int Count { get; private set; }
    public int Capacity { get; private set; } = 8;

    public ref Tileset2DState TilesetState =>
        ref ShadowMapTilesetState.Get<Tileset2DState>();

    public EntityRef ShadowMapTilesetEntity { get; private set; }
    public EntityRef ShadowMapTilesetState { get; private set; }

    private ref UniformHeader Header => ref *(UniformHeader*)_uniformPointer;
    private Span<SamplerSlot> SamplerSlots =>
        new((void*)_samplerSlotsPointer, SamplerSlotCount);

    private int _resolution = 1024;
    private RTileset2D? _shadowMapTileRecord;

    private readonly Dictionary<EntityRef, ShadowMapHandle> _allocated = [];
    private readonly Stack<int> _released = [];

    private BufferHandle _uniformBufferHandle;
    private IntPtr _uniformPointer;
    private IntPtr _samplerSlotsPointer;

    private EntityRef? _primaryLight;
    private EntityRef? _secondaryLight;

    [AllowNull] private RenderFramer _renderFramer;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _renderFramer = world.GetAddon<RenderFramer>();
        _renderFramer.Start(CreateUniformBuffer);
        UpdateShadowMapTileset();
    }

    public override void OnUninitialize(World world)
    {
        base.OnUninitialize(world);
        ShadowMapTilesetEntity.Dispose();
    }

    public ShadowMapHandle Allocate(in EntityRef lightEntity)
    {
        ref var handle = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _allocated, lightEntity, out bool exists);
        if (exists) {
            throw new NaguleInternalException("Light shadow map has been allocated");
        }
        if (_released.TryPop(out int index)) {
            handle = new(index);
        }
        else {
            handle = new(Count);
            Count++;
            if (Count >= Capacity) {
                Capacity *= 2;
                UpdateShadowMapTileset();
            }
        }
        return handle;
    }

    public bool Release(in EntityRef lightEntity)
    {
        if (!_allocated.Remove(lightEntity, out var handle)) {
            return false;
        }
        Count--;
        var index = handle.Value;
        if (index == Count) {
            if (Count < Capacity / 2) {
                Capacity /= 2;
                UpdateShadowMapTileset();
            }
        }
        else {
            _released.Push(index);
        }
        return true;
    }

    public bool Contains(in EntityRef lightEntity)
        => _allocated.ContainsKey(lightEntity);
    
    private void CreateUniformBuffer()
    {
        _uniformBufferHandle = new(GL.GenBuffer());
        GL.BindBuffer(BufferTargetARB.UniformBuffer, _uniformBufferHandle.Handle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer,
            (int)UniformBlockBinding.ShadowMapLibrary, _uniformBufferHandle.Handle);

        _uniformPointer = GLUtils.InitializeBuffer(
            BufferTargetARB.UniformBuffer,
            UniformHeader.MemorySize + SamplerSlotCount * SamplerSlot.MemorySize);
        _samplerSlotsPointer = _uniformPointer + UniformHeader.MemorySize;
        
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        Header.ShadowMapWidth = _resolution;
        Header.ShadowMapHeight = _resolution;
        Header.PrimaryLightShadowMapIndex = -1;
        Header.SecondaryLightShadowMapIndex = -1;

        foreach (ref var slot in SamplerSlots) {
            slot.LightIndex = -1;
            slot.NextSlotIndex = MaximumSamplerCount;
        }
    }

    private void UpdateShadowMapTileset()
    {
        if (ShadowMapTilesetEntity.Valid) {
            ShadowMapTilesetEntity.Dispose();
        }

        _shadowMapTileRecord = new() {
            Image = new RImage {
                PixelFormat = PixelFormat.Depth
            },
            TileWidth = _resolution,
            TileHeight = _resolution,
            Count = Capacity
        };

        ShadowMapTilesetEntity = Tileset2D.CreateEntity(
            World, _shadowMapTileRecord, AssetLife.Persistent);
        ShadowMapTilesetState = ShadowMapTilesetEntity.GetStateEntity();

        OnTilesetRecreated?.Invoke();
    }
}