namespace Nagule.Graphics.ShadowMapping;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

using Nagule.Graphics.Backends.OpenTK;

public record struct ShadowMapHandle(int Index);

public unsafe class ShadowMapLibrary : ViewBase
{
    public const int MaximumSamplerCount = 127;
    public const int SamplerCellarSlotCount = 109;

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

    public RTileset2D TilesetRecord { get; private set; } = null!;

    public int Count { get; private set; }
    public int Capacity { get; private set; } = 8;

    public ref Tileset2DState TilesetState =>
        ref ShadowMapTilesetState.Get<Tileset2DState>();

    public EntityRef ShadowMapTilesetEntity { get; private set; }
    public EntityRef ShadowMapTilesetState { get; private set; }

    private ref UniformHeader Header => ref *(UniformHeader*)_uniformPointer;
    private Span<SamplerSlot> SamplerSlots =>
        new((void*)_samplerSlotsPointer, MaximumSamplerCount);

    private int _resolution = 1024;

    private readonly Dictionary<AssetId, ShadowMapHandle> _allocated = [];
    private readonly Stack<int> _released = [];

    private BufferHandle _uniformBufferHandle;
    private IntPtr _uniformPointer;
    private IntPtr _samplerSlotsPointer;

    private EntityRef? _primaryLight;
    private EntityRef? _secondaryLight;

    private RenderFramer _renderFramer = null!;

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
            _allocated, lightEntity.GetAssetId(), out bool exists);
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
        if (!_allocated.Remove(lightEntity.GetAssetId(), out var handle)) {
            return false;
        }
        Count--;
        var index = handle.Index;
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
        => _allocated.ContainsKey(lightEntity.GetAssetId());

    private void AddShadowMapSampler(EntityRef lightEntity, ShadowMapHandle handle)
    {
        ref var light = ref lightEntity.Get<Light3D>();
        var shadowStrength = light.ShadowStrength;
        var stateEntity = lightEntity.GetStateEntity();

        _renderFramer.Enqueue(lightEntity, () => {
            int lightIndex = stateEntity.Get<Light3DState>().Index;
            int slotIndex = lightIndex % SamplerCellarSlotCount;
            var slots = SamplerSlots;

            while (true) {
                ref var slot = ref slots[slotIndex];

                int slotLightIndex = slot.LightIndex;
                if (slotLightIndex == -1) {
                    slot.LightIndex = lightIndex;
                    slot.Sampler.Index = handle.Index;
                    slot.Sampler.Strength = shadowStrength;
                    return;
                }

                slotIndex = slot.NextSlotIndex;

                if (slotIndex == -1) {
                    int emptySlotIndex = -1;
                    for (int i = MaximumSamplerCount - 1; i >= 0; --i) {
                        if (slots[i].LightIndex == -1 && slots[i].NextSlotIndex == -1) {
                            emptySlotIndex = i;
                            break;
                        }
                    }
                    if (emptySlotIndex != -1) {
                        ref var emptySlot = ref slots[emptySlotIndex];
                        emptySlot.LightIndex = lightIndex;
                        emptySlot.Sampler.Index = handle.Index;
                        emptySlot.Sampler.Strength = shadowStrength;
                        slot.NextSlotIndex = emptySlotIndex;
                    }
                    return;
                }
            }
        });
    }

    private void RemoveShadowSampler(EntityRef lightEntity)
    {
        var stateEntity = lightEntity.GetStateEntity();

        _renderFramer.Enqueue(lightEntity, () => {
            int lightIndex = stateEntity.Get<Light3DState>().Index;
            int slotIndex = lightIndex % MaximumSamplerCount;

            while (true) {
                ref var slot = ref SamplerSlots[slotIndex];
                int slotLightIndex = slot.LightIndex;
                if (slotLightIndex == -1) {
                    return;
                }
                if (slotLightIndex == lightIndex) {
                    slot.LightIndex = -1;
                }
                slotIndex = slot.NextSlotIndex;
            }
        });
    }
    
    private void CreateUniformBuffer()
    {
        _uniformBufferHandle = new(GL.GenBuffer());
        GL.BindBuffer(BufferTargetARB.UniformBuffer, _uniformBufferHandle.Handle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer,
            (int)UniformBlockBinding.ShadowMapLibrary, _uniformBufferHandle.Handle);

        _uniformPointer = GLUtils.InitializeBuffer(
            BufferTargetARB.UniformBuffer,
            UniformHeader.MemorySize + MaximumSamplerCount * SamplerSlot.MemorySize);
        _samplerSlotsPointer = _uniformPointer + UniformHeader.MemorySize;
        
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        Header.ShadowMapWidth = _resolution;
        Header.ShadowMapHeight = _resolution;
        Header.PrimaryLightShadowMapIndex = -1;
        Header.SecondaryLightShadowMapIndex = -1;

        foreach (ref var slot in SamplerSlots) {
            slot.LightIndex = -1;
            slot.NextSlotIndex = -1;
        }
    }

    private void UpdateShadowMapTileset()
    {
        if (ShadowMapTilesetEntity.Valid) {
            ShadowMapTilesetEntity.Dispose();
        }

        TilesetRecord = new() {
            Image = new RImage {
                PixelFormat = PixelFormat.Depth
            },
            TileWidth = _resolution,
            TileHeight = _resolution,
            Count = Capacity
        };

        ShadowMapTilesetEntity = World.AcquireAsset(TilesetRecord, AssetLife.Persistent);
        ShadowMapTilesetState = ShadowMapTilesetEntity.GetStateEntity();

        OnTilesetRecreated?.Invoke();
    }
}