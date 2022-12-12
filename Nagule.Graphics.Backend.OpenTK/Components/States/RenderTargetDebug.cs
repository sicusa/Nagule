namespace Nagule.Graphics.Backend.OpenTK.Graphics;

public enum DisplayMode
{
    Color,
    TransparencyAccum,
    TransparencyAlpha,
    Depth,
    Clusters
}

public struct RenderTargetDebug : IPooledComponent
{
    public DisplayMode DisplayMode;
}