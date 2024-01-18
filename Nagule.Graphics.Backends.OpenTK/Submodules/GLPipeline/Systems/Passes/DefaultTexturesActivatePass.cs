namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DefaultTexturesActivatePass : RenderPassSystemBase
{
    private EntityRef _whiteTex;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var tex2DManager = world.GetAddon<Texture2DManager>();
        _whiteTex = tex2DManager.Acquire(RTexture2D.White);

        var whiteTexStateEntity = _whiteTex.GetStateEntity();

        RenderFramer.Start(() => {
            ref var whiteTexState = ref whiteTexStateEntity.Get<Texture2DState>();
            if (!whiteTexState.Loaded) { return NextFrame; }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, whiteTexState.Handle.Handle);

            return NextFrame;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        _whiteTex.Dispose();
    }
}