namespace Nagule.Graphics;

using Sia;

public abstract class RenderAddonBase : IAddon
{
    protected World World { get; private set; } = null!;
    protected RenderFramer RenderFramer { get; private set; } = null!;

    private bool _stopped;

    public virtual void OnInitialize(World world)
    {
        _stopped = false;

        World = world;
        RenderFramer = world.GetAddon<RenderFramer>();
        RenderFramer.Start(() => {
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