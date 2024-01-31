namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class GLMesh3DInstanceUpdator : GraphicsUpdatorBase<EntityRef, GLMesh3DInstanceUpdator.Entry>
{
    public record struct Entry(EntityRef Entity, Matrix4x4 WorldMat)
        : IGraphicsUpdatorEntry<EntityRef, Entry>
    {
        public readonly EntityRef Key => Entity;

        public static void Record(in EntityRef entity, ref Entry value)
        {
            var worldMat = entity.GetFeatureNode<Transform3D>().World;
            value = new(entity, worldMat);
        }
    }

    [AllowNull] private GLMesh3DInstanceLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<GLMesh3DInstanceLibrary>();
    }

    protected override void UpdateEntry(in EntityRef e, in Entry entry)
    {
        ref var instanceEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_lib.InstanceEntries, e);
        if (!Unsafe.IsNullRef(ref instanceEntry)) {
            instanceEntry.Group.InstanceBuffer[instanceEntry.Index] = entry.WorldMat;
        }
    }
}