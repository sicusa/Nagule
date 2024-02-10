namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DefaultTexturesActivatePass : RenderPassBase
{
    private EntityRef _whiteTex;
    private EntityRef _whiteTexState;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _whiteTex = MainWorld.AcquireAsset(RTexture2D.White);
        _whiteTexState = _whiteTex.GetStateEntity();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var whiteTexState = ref _whiteTexState.Get<Texture2DState>();
        if (!whiteTexState.Loaded) { return; }

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, whiteTexState.Handle.Handle);
    }
}