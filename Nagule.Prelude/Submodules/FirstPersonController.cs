namespace Nagule.Prelude;

using System.Numerics;
using Sia;

[SiaTemplate(nameof(FirstPersonController))]
[NaAsset]
public record RFirstPersonController : RFeatureBase
{
    public float Rate { get; init; } = 10;
    public float Sensitivity { get; init; } = 0.005f;
}

public struct FirstPersonControllerState()
{
    public bool Moving;
    public Vector2 Position;
    public Vector3 SmoothDir;
}

public class FirstPersonControllerSystem()
    : SystemBase(
        matcher: Matchers.Of<FirstPersonController>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var windowEntity = world.GetAddon<PrimaryWindow>().Entity;
        var peripheral = world.GetAddon<Peripheral>();
        var deltaTime = world.GetAddon<SimulationFramer>().DeltaTime;

        foreach (var entity in query) {
            if (!entity.IsFeatureEnabled()) {
                return;
            }

            ref var controller = ref entity.Get<FirstPersonController>();
            var scaledRate = controller.Rate * deltaTime;

            ref var state = ref entity.GetState<FirstPersonControllerState>();
            ref var pos = ref state.Position;
            ref var moving = ref state.Moving;
            ref var smoothDir = ref state.SmoothDir;

            ref var window = ref windowEntity.Get<Window>();
            var windowSize = new Vector2(window.Size.Item1, window.Size.Item2) / 2;

            var cameraNode = entity.GetFeatureNode();
            ref var cameraTrans = ref cameraNode.Get<Transform3D>();

            ref var keyboard = ref peripheral.Keyboard;
            ref var mouse = ref peripheral.Mouse;

            pos = Vector2.Lerp(pos, (mouse.Position - windowSize) * controller.Sensitivity, scaledRate);
            cameraNode.Modify(ref cameraTrans,
                new Transform3D.SetRotation(Quaternion.CreateFromYawPitchRoll(-pos.X, -pos.Y, 0)));

            var direction = Vector3.Zero;
            bool movedThisFrame = false;

            if (keyboard.IsKeyPressed(Key.W)) {
                direction += cameraTrans.WorldForward;
                movedThisFrame = true;
                moving = true;
            }
            if (keyboard.IsKeyPressed(Key.S)) {
                direction -= cameraTrans.WorldForward;
                movedThisFrame = true;
                moving = true;
            }
            if (keyboard.IsKeyPressed(Key.A)) {
                direction -= cameraTrans.WorldRight;
                movedThisFrame = true;
                moving = true;
            }
            if (keyboard.IsKeyPressed(Key.D)) {
                direction += cameraTrans.WorldRight;
                movedThisFrame = true;
                moving = true;
            }
            if (moving) {
                smoothDir = Vector3.Lerp(smoothDir, direction, scaledRate);
                if (!movedThisFrame && smoothDir.Length() < 0.001f) {
                    moving = false;
                    smoothDir = Vector3.Zero;
                }
                else {
                    cameraNode.Modify(ref cameraTrans,
                        new Transform3D.SetPosition(cameraTrans.Position + smoothDir * deltaTime * 5));
                }
            }
        }
    }
}

[NaAssetModule<RFirstPersonController, FirstPersonControllerState>]
public partial class FirstPersonControllerModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<FirstPersonControllerSystem>());