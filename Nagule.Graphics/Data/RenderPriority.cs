namespace Nagule.Graphics;

public enum RenderPriority
{
    DepthSampler = -10000,
    Default = 0
}

public static class RenderPriorityExtensions
{
    public static RenderPriority From(int value) => (RenderPriority)value;
}