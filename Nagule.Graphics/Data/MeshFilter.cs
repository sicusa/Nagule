namespace Nagule.Graphics;

[Flags]
public enum MeshFilter
{
    None,

    // RenderMode
    Opaque = 1,
    Transparent = 2,
    Cutoff = 4,
    Blending = 8,

    // LightingMode
    NoFullLighting = 16,
    NoLocalLighting = 32,
    NoGlobalLighting = 64,
    NoUnlit = 128,

    // IsTwoSided
    NoTwoSided = 256,

    All = Opaque | Transparent | Cutoff | Blending
}

public static class MeshFilterExtensions
{
    public static bool Check(this MeshFilter filter, RenderMode renderMode, LightingMode lightingMode, bool isTwoSided)
    {
        var failed = renderMode switch {
            RenderMode.Opaque => (filter & MeshFilter.Opaque) == 0,
            RenderMode.Transparent => (filter & MeshFilter.Transparent) == 0,
            RenderMode.Cutoff => (filter & MeshFilter.Cutoff) == 0,
            RenderMode.Blending => (filter & MeshFilter.Blending) == 0,
            _ => true
        };
        if (failed) { return false; }

        failed = lightingMode switch {
            LightingMode.Full => (filter & MeshFilter.NoFullLighting) != 0,
            LightingMode.Global => (filter & MeshFilter.NoGlobalLighting) != 0,
            LightingMode.Local => (filter & MeshFilter.NoLocalLighting) != 0,
            LightingMode.Unlit => (filter & MeshFilter.NoUnlit) != 0,
            _ => true
        };
        if (failed) { return false; }

        return true;
    }
}