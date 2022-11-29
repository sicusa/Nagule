namespace Nagule.Graphics;

using System.Numerics;

public struct CameraMatrices : IPooledComponent
{
    public Matrix4x4 Projection = Matrix4x4.Identity;

    public CameraMatrices() {}
}