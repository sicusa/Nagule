namespace Nagule.Graphics.Backends.OpenTK;

using System.Runtime.InteropServices;
using Sia;

public record struct ShadowMapHandle(int Value);

public class ShadowMapLibrary : ViewBase
{
    public event Action? OnTilesetRecreated;

    public int Resolution {
        get => _resolution;
        set {
            _resolution = value;
            UpdateShadowMapTileset();
        }
    }

    public int Count { get; private set; }
    public int Capacity { get; private set; } = 8;

    public ref Tileset2DState TilesetState =>
        ref ShadowMapTilesetState.Get<Tileset2DState>();

    public EntityRef ShadowMapTilesetEntity { get; private set; }
    public EntityRef ShadowMapTilesetState { get; private set; }

    private int _resolution = 1024;
    private RTileset2D? _shadowMapTileRecord;

    private readonly Dictionary<EntityRef, ShadowMapHandle> _allocated = [];
    private readonly Stack<int> _released = [];

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
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