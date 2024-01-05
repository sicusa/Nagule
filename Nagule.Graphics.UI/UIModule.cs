namespace Nagule.Graphics.UI;

using Sia;

public class UIModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<ImGuiSystems>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<ImGuiEventDispatcher>(world);
    }
}