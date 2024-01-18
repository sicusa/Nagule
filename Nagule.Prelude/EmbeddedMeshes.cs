using Nagule.Graphics;

namespace Nagule.Prelude;

public static class EmbeddedMeshes
{
    public static RMesh3D Cube { get; } = Load("models.cube.glb");
    public static RMesh3D Plane { get; } = Load("models.plane.glb");
    public static RMesh3D Sphere { get; } = Load("models.sphere.glb");
    public static RMesh3D Torus { get; } = Load("models.torus.glb");
    public static RMesh3D Cone { get; } = Load("models.cone.glb");
    public static RMesh3D Cylinder { get; } = Load("models.cylinder.glb");
    
    private static RMesh3D Load(string path)
        => (EmbeddedAssets.LoadInternal<RModel3D>(path)
            .RootNode.Features.First() as RMesh3D)!;
}