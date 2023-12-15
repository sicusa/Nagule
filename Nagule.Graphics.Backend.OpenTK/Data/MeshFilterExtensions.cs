namespace Nagule.Graphics.Backend.OpenTK;

public static class MeshFilterExtensions
{
    public static bool Check(this MeshFilter filter, in MaterialState materialState)
        => filter.Check(materialState.RenderMode, materialState.LightingMode, materialState.IsTwoSided);
}