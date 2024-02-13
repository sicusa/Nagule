namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Mesh3DInstanceUpdator : GLBufferUpdatorBase<EntityRef, Mesh3DInstanceUpdator.Entry>
{
    public record struct Entry(EntityRef Entity, Matrix4x4 WorldMat, LayerMask LayerMask)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => Entity;

        public static bool Record(in EntityRef entity, ref Entry value)
        {
            value.Entity = entity;

            ref var feature = ref entity.Get<Feature>();
            if (!feature.IsEnabled) {
                value.WorldMat = default;
                value.LayerMask = default;
                return true;
            }

            var nodeEntity = feature.Node;
            var mask = nodeEntity.Get<Node3D>().Layer.Mask;

            if (entity.Get<Mesh3D>().IsShadowCaster) {
                mask |= GLInternalLayers.ShadowCaster.Mask;
            }

            value.WorldMat = nodeEntity.Get<Transform3D>().World;
            value.LayerMask = mask;
            return true;
        }
    }

    private Mesh3DInstanceLibrary _lib = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Mesh3DInstanceLibrary>();
    }

    protected override void UpdateEntry(in EntityRef e, in Entry entry)
    {
        ref var instanceEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_lib.InstanceEntries, e);
        if (!Unsafe.IsNullRef(ref instanceEntry)) {
            ref var instance = ref instanceEntry.Group.InstanceBuffer[instanceEntry.Index];
            instance.ObjectToWorld = entry.WorldMat;
            instance.LayerMask = entry.LayerMask;
        }
    }
}