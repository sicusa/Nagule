using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public abstract class GLGraphicsUpdatorBase<TKey, TEntry>
    : GraphicsUpdatorBase<TKey, TEntry>, IGLGraphicsUpdator
    where TKey : notnull
    where TEntry : struct, IGraphicsUpdatorEntry<TKey, TEntry>
{
    private GLSync _sync;

    protected override void OnRender()
    {
        GLUtils.FenceSync(ref _sync);
        base.OnRender();
    }

    public void WaitSync()
        => GLUtils.WaitSync(_sync);
}