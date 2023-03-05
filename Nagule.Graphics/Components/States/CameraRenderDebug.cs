namespace Nagule.Graphics;

public enum DisplayMode
{
    Color,
    TransparencyAccum,
    TransparencyAlpha,
    Depth,
    Clusters
}

public struct CameraRenderDebug : IHashComponent
{
    public DisplayMode DisplayMode;
}