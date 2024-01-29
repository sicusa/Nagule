namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class RenderAddonBase : IAddon
{
    [AllowNull] protected RenderFramer RenderFramer { get; private set; }
    private bool _stopped;

    public virtual void OnInitialize(World world)
    {
        _stopped = false;

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