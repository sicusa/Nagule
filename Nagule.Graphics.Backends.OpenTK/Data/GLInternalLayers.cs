namespace Nagule.Graphics.Backends.OpenTK;

internal static class GLInternalLayers
{
    public static readonly Layer ShadowCaster = Layer.From<ShadowCasterLayer>();

    public class ShadowCasterLayer : ILayer;
}