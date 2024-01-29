namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public record struct ShadowMapHandle(int Value);

public class ShadowMapLibrary : ViewBase
{
    private struct Entry
    {
        public ShadowMapHandle Handle;
        public Matrix4x4 Projection;
    }

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

    private readonly Dictionary<EntityRef, Entry> _allocated = [];
    private readonly Stack<int> _released = [];

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        world.GetAddon<SimulationFramer>().Start(UpdateShadowMapTileset);

        void UpdateLightProjection(in EntityRef e)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_allocated, e);
            if (!Unsafe.IsNullRef(ref entry)) {
                entry.Projection = CreateLightProjection(e)!.Value;
            }
        }

        Listen((in EntityRef e, in Light3D.SetRange cmd) => UpdateLightProjection(e));
        Listen((in EntityRef e, in Light3D.SetInnerConeAngle cmd) => UpdateLightProjection(e));
        Listen((in EntityRef e, in Light3D.SetOuterConeAngle cmd) => UpdateLightProjection(e));
        Listen((in EntityRef e, in Light3D.SetShadowNearPlane cmd) => UpdateLightProjection(e));
    }

    public override void OnUninitialize(World world)
    {
        base.OnUninitialize(world);
        ShadowMapTilesetEntity.Dispose();
    }

    public ShadowMapHandle? Allocate(in EntityRef lightEntity)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _allocated, lightEntity, out bool exists);
        if (exists) {
            throw new NaguleInternalException("Light shadow map has been allocated");
        }
        var proj = CreateLightProjection(lightEntity);
        if (proj == null) {
            return null;
        }
        if (_released.TryPop(out int index)) {
            entry = new Entry {
                Handle = new(index),
                Projection = proj.Value
            };
        }
        else {
            entry = new Entry {
                Handle = new(Count),
                Projection = proj.Value
            };
            Count++;
            if (Count >= Capacity) {
                Capacity *= 2;
                UpdateShadowMapTileset();
            }
        }
        return entry.Handle;
    }

    public bool Release(in EntityRef lightEntity)
    {
        if (!_allocated.Remove(lightEntity, out var entry)) {
            return false;
        }
        Count--;
        var index = entry.Handle.Value;
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

    public ref Matrix4x4 GetProjection(in EntityRef lightEntity)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(
            _allocated, lightEntity);
        if (Unsafe.IsNullRef(ref entry)) {
            throw new NaguleInternalException("Light shadow map not allocated");
        }
        return ref entry.Projection;
    }

    private static Matrix4x4? CreateLightProjection(in EntityRef lightEntity)
    {
        ref var light = ref lightEntity.Get<Light3D>();
        return light.Type switch {
            LightType.Directional =>
                Matrix4x4.CreateOrthographic(10, 10, light.ShadowNearPlane, light.Range),
            LightType.Spot =>
                Matrix4x4.CreatePerspective(10, 10, light.ShadowNearPlane, light.Range),
            _ => null,
        };
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