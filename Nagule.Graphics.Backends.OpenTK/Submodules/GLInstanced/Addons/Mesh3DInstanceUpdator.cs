namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Mesh3DInstanceUpdator : GLBufferUpdatorBase<AssetId, Mesh3DInstanceUpdator.Entry>
{
    public record struct Entry(AssetId AssetId, Matrix4x4 WorldMat, LayerMask LayerMask)
        : IGraphicsUpdatorEntry<AssetId, Entry>
    {
        public readonly AssetId Key => AssetId;

        public static void Record(in EntityRef entity, out Entry value)
        {
            value = new Entry {
                AssetId = entity.GetAssetId()
            };

            ref var feature = ref entity.Get<Feature>();
            if (!feature.IsEnabled) {
                value.WorldMat = default;
                value.LayerMask = default;
                return;
            }

            var nodeEntity = feature.Node;
            var mask = nodeEntity.Get<Node3D>().Layer.Mask;

            if (entity.Get<Mesh3D>().IsShadowCaster) {
                mask |= GLInternalLayers.ShadowCaster.Mask;
            }

            value.WorldMat = nodeEntity.Get<Transform3D>().WorldMatrix;
            value.LayerMask = mask;
        }
    }

    private Mesh3DInstanceLibrary _lib = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Mesh3DInstanceLibrary>();
    }

    protected override void UpdateEntry(AssetId id, in Entry entry)
    {
        ref var instanceEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_lib.InstanceEntries, id);
        if (!Unsafe.IsNullRef(ref instanceEntry)) {
            ref var instance = ref instanceEntry.Group.InstanceBuffer[instanceEntry.Index];
            instance.ObjectToWorld = entry.WorldMat;
            instance.LayerMask = entry.LayerMask;
        }
    }
}