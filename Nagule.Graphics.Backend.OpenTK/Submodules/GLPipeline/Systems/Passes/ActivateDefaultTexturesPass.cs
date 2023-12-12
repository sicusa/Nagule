namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class ActivateDefaultTexturesPass : RenderPassSystemBase
{
    private EntityRef _whiteTex;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var tex2DManager = world.GetAddon<Texture2DManager>();
        _whiteTex = tex2DManager.Acquire(Texture2DAsset.White);

        RenderFrame.Start(() => {
            ref var whiteTexState = ref tex2DManager.RenderStates.GetOrNullRef(_whiteTex);
            if (Unsafe.IsNullRef(ref whiteTexState)) {
                return ShouldStop;
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, whiteTexState.Handle.Handle);
            return ShouldStop;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        world.Destroy(_whiteTex);
    }
}