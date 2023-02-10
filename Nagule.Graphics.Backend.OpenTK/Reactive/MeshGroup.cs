namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;

using Aeco;
using Aeco.Reactive;

public class MeshGroup : Group<MeshData>
{
    private List<Guid> _occluderMeshes = new();
    private List<Guid> _opaqueMeshes = new();
    private List<Guid> _nonoccluderMeshes = new();
    private List<Guid> _nonoccluderOpaqueMeshes = new();
    private List<Guid> _blendingMeshes = new();
    private List<Guid> _transparentMeshes = new();

    public ReadOnlySpan<Guid> GetMeshIds(MeshFilter filter)
        => filter switch {
            MeshFilter.All => AsSpan(),
            MeshFilter.Opaque => CollectionsMarshal.AsSpan(_opaqueMeshes),
            MeshFilter.Occluder => CollectionsMarshal.AsSpan(_occluderMeshes),
            MeshFilter.Nonoccluder => CollectionsMarshal.AsSpan(_nonoccluderMeshes),
            MeshFilter.NonoccluderOpaque => CollectionsMarshal.AsSpan(_nonoccluderOpaqueMeshes),
            MeshFilter.Blending => CollectionsMarshal.AsSpan(_blendingMeshes),
            MeshFilter.Transparent => CollectionsMarshal.AsSpan(_transparentMeshes),
            _ => throw new ArgumentException("Invalid mesh filter")
        };

    public override void Refresh(IReadableDataLayer<IComponent> dataLayer)
    {
        base.Refresh(dataLayer);

        _occluderMeshes.Clear();
        _opaqueMeshes.Clear();
        _nonoccluderMeshes.Clear();
        _nonoccluderOpaqueMeshes.Clear();
        _blendingMeshes.Clear();
        _transparentMeshes.Clear();

        foreach (var id in this) {
            ref readonly var meshData = ref dataLayer.Inspect<MeshData>(id);

            if (meshData.IsOccluder) {
                _occluderMeshes.Add(id);
                _opaqueMeshes.Add(id);
                continue;
            }

            _nonoccluderMeshes.Add(id);

            switch (meshData.RenderMode) {
            case RenderMode.Transparent:
                _transparentMeshes.Add(id);
                continue;
            case RenderMode.Multiplicative:
            case RenderMode.Additive:
                _blendingMeshes.Add(id);
                continue;
            }

            _opaqueMeshes.Add(id);
            _nonoccluderOpaqueMeshes.Add(id);
        }
    }
}