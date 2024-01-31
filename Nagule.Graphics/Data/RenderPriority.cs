namespace Nagule.Graphics;

public enum RenderPriority
{
    Default = -1
}

public static class RenderPriorityExtensions
{
    public static RenderPriority From(int value) => (RenderPriority)value;
}