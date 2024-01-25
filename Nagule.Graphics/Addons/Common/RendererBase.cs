namespace Nagule.Graphics;

using Sia;

public abstract class RendererBase : IAddon
{
    private bool _stopped;

    public virtual void OnInitialize(World world)
    {
        _stopped = false;

        world.GetAddon<RenderFramer>().Start(() => {
            if (_stopped) {
                return true;
            }
            OnRender();
            return false;
        });
    }

    public virtual void OnUninitialize(World world)
    {
        _stopped = true;
    }

    protected abstract void OnRender();
}