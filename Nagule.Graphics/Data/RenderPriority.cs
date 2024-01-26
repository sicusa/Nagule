namespace Nagule.Graphics;

public enum RenderPriority
{
    Default = -1,
    ShadowMap = -1000
}

public static class RenderPriorityExtensions
{
    public static RenderPriority From(int value) => (RenderPriority)value;
}