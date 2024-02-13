using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public abstract class GLBufferUpdatorBase<TKey, TEntry>
    : GraphicsUpdatorBase<TKey, TEntry>, IGLBufferUpdator
    where TKey : notnull
    where TEntry : struct, IGraphicsUpdatorEntry<TKey, TEntry>
{
    private GLSync _sync;

    protected override void OnRender()
    {
        if (_sync != default) {
            GLUtils.WaitSync(_sync);
        }
        base.OnRender();
    }

    public void LockBuffer()
        => GLUtils.FenceSync(ref _sync);
}