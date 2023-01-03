namespace Nagule.Graphics;

public enum DisplayMode
{
    Color,
    TransparencyAccum,
    TransparencyAlpha,
    Depth,
    Clusters
}

public struct CameraRenderDebug : IPooledComponent
{
    public DisplayMode DisplayMode;
}