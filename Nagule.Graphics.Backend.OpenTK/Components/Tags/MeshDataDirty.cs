namespace Nagule.Graphics.Backend.OpenTK;

public struct MeshDataDirty : ITagComponent
{
    public static Guid Id { get; } = Guid.NewGuid();
}