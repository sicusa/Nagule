namespace Nagule.Graphics;

public enum RenderMode
{
    Opaque,
    Transparent,
    Cutoff,
    Additive,
    Multiplicative,

    Unlit,
    UnlitTransparent,
    UnlitCutoff,
    UnlitAdditive,
    UnlitMultiplicative
}

public static class RenderModeHelper
{
    public static bool IsTransparent(RenderMode mode)
        => mode == RenderMode.Transparent || mode == RenderMode.UnlitTransparent;

    public static bool IsBlending(RenderMode mode)
        => mode == RenderMode.Additive || mode == RenderMode.Multiplicative
            || mode == RenderMode.UnlitAdditive || mode == RenderMode.UnlitMultiplicative;
}