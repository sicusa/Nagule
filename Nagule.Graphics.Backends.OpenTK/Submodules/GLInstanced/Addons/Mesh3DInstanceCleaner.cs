namespace Nagule.Graphics.Backends.OpenTK;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Mesh3DInstanceCleaner : ViewBase<TypeUnion<Mesh3D>>
{
    protected override void OnEntityAdded(in EntityRef entity) {}
    protected override void OnEntityRemoved(in EntityRef entity)
    {
        var id = entity.GetAssetId();

        var lib = World.GetAddon<Mesh3DInstanceLibrary>();
        var renderFramer = World.GetAddon<RenderFramer>();

        renderFramer.Start(() => {
            var groups = lib.Groups;
            var instanceEntries = lib.InstanceEntries;

            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(instanceEntries, id);
            if (Unsafe.IsNullRef(ref entry)) {
                throw new NaguleInternalException("This should not happen!");
            }
            var group = entry.Group;

            if (group.Count == 1) {
                group.Dispose();
                groups.Remove(group.Key);
            }
            else {
                group.Remove(entry.Index);
                if (entry.Index != group.Count) {
                    instanceEntries[group.AssetIds[entry.Index]] = (group, entry.Index);
                }
            }
            return true;
        });
    }
}