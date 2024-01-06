namespace Nagule.Prelude;

using System.Numerics;
using Sia;

public class FirstPersonControllerManager
    : AssetManager<FirstPersonController, RFirstPersonController, FirstPersonControllerState>;

public class FirstPersonControllerSystem()
    : SystemBase(
        matcher: Matchers.Of<FirstPersonController>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            windowEntity: world.GetAddon<PrimaryWindow>().Entity,
            peripheral: world.GetAddon<Peripheral>(),
            deltaTime: world.GetAddon<SimulationFrame>().DeltaTime
        );
        query.ForEach(data, static (d, entity) => {
            ref var keyboard = ref d.peripheral.Keyboard;
            ref var mouse = ref d.peripheral.Mouse;

            ref var state = ref entity.GetState<FirstPersonControllerState>();

            if (keyboard.IsKeyDown(Key.C)) {
                state.Active = !state.Active;
            }
            if (!state.Active) {
                return;
            }

            ref var pos = ref state.Position;
            ref var moving = ref state.Moving;
            ref var smoothDir = ref state.SmoothDir;

            ref var window = ref d.windowEntity.Get<Window>();
            var windowSize = new Vector2(window.Size.Item1, window.Size.Item2) / 2;

            ref var controller = ref entity.Get<FirstPersonController>();
            var scaledRate = controller.Rate * d.deltaTime;
            state.Position = Vector2.Lerp(pos, (mouse.Position - windowSize) * controller.Sensitivity, scaledRate);

            var cameraNode = entity.GetFeatureNode();
            ref var cameraTrans = ref cameraNode.Get<Transform3D>();
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
                        new Transform3D.SetPosition(cameraTrans.Position + smoothDir * d.deltaTime * 5));
                }
            }
        });
    }
}

public class FirstPersonControllerModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<FirstPersonControllerSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<FirstPersonControllerManager>(world);
    }
}