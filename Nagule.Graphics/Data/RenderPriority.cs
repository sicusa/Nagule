namespace Nagule.Graphics;

public enum RenderPriority
{
    Default = 0
}

public static class RenderPriorityExtensions
{
    public static RenderPriority From(int value) => (RenderPriority)value;
}