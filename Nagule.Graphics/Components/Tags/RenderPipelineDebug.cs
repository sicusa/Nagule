namespace Nagule.Graphics;

public enum DisplayMode
{
    Color,
    TransparencyAccum,
    TransparencyAlpha,
    Depth,
    Clusters
}

public struct RenderPipelineDebug : IPooledComponent
{
    public DisplayMode DisplayMode;
}