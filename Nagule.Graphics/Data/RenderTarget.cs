namespace Nagule.Graphics;

public abstract record RenderTarget
{
    public sealed record RenderTexture(RRenderTexture2D Texture) : RenderTarget;
    public sealed record Window(int Index) : RenderTarget;

    private record NoneTarget : RenderTarget;

    public static RenderTarget None { get; } = new NoneTarget();
    public static Window PrimaryWindow { get; } = new(0);

    public static implicit operator RenderTarget(RRenderTexture2D texture)
        => new RenderTexture(texture);
}