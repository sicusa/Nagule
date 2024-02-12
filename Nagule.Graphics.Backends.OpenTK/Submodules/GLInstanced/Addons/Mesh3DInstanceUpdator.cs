namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Mesh3DInstanceUpdator : GLGraphicsUpdatorBase<EntityRef, Mesh3DInstanceUpdator.Entry>
{
    public record struct Entry(EntityRef Entity, Matrix4x4 WorldMat)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => Entity;

        public static bool Record(in EntityRef entity, ref Entry value)
        {
            ref var feature = ref entity.Get<Feature>();
            if (feature.IsEnabled) {
                var worldMat = feature.Node.Get<Transform3D>().World;
                value = new(entity, worldMat);
            }
            else {
                value = new(entity, default);
            }
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
            instanceEntry.Group.InstanceBuffer[instanceEntry.Index] = entry.WorldMat;
        }
    }
}