namespace Nagule.Examples;

using Sia;

using Nagule;
using Nagule.Graphics;
using Nagule.Graphics.Backend.OpenTK;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

public static class Example
{
    public record struct Rotator(float Speed);

    private class RotatorSystem : SystemBase
    {
        public RotatorSystem()
        {
            Matcher = Matchers.Of<Transform3D, Rotator>();
        }

        public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        {
            var frame = world.GetAddon<SimulationFrame>();
            query.ForEach(frame, static (frame, entity) => {
                ref var transform = ref entity.Get<Transform3D>();
                ref var rotator = ref entity.Get<Rotator>();
                entity.Modify(new Transform3D.SetPosition(
                    transform.Position + transform.Forward * frame.DeltaTime * rotator.Speed));
                entity.Modify(new Transform3D.SetRotation(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, frame.Time)));
            });
        }
    }

    private class ExampleSystem : SystemBase
    {
        private float _rate = 10;
        private float _sensitivity = 0.005f;
        private Vector2 _pos = Vector2.Zero;
        private Vector3 _smoothDir = Vector3.Zero;
        private bool _moving = false;
        private bool _controlActive = true;

        [AllowNull] private SimulationFrame _frame;

        private EntityRef _sceneNode;
        private EntityRef _toriNode;

        public ExampleSystem()
        {
            Matcher = Matchers.Any;
        }

        public override void Initialize(World world, Scheduler scheduler)
        {
            base.Initialize(world, scheduler);
            _frame = world.GetAddon<SimulationFrame>();

            var cameraId = Guid.NewGuid();
            var buildingId = Guid.NewGuid();

            var scene = new Node3DAsset {
                Children = [
                    EmbeddedAssets.Load<Model3DAsset>("Nagule.Examples.Embedded.Models.library_earthquake.glb").RootNode with {
                        Id = buildingId,
                        Position = new Vector3(0, 0, 0)
                    },
                    new Node3DAsset {
                        Name = "Lighting",
                        Rotation = Quaternion.CreateFromYawPitchRoll(-45f, -45f, 0f),
                        Features = [
                            new Light3DAsset {
                                Type = LightType.Directional,
                                Color = new(1f, 1f, 1f, 0.032f)
                            },
                            new Light3DAsset {
                                Type = LightType.Ambient,
                                Color = new(1f, 1f, 1f, 0.3f)
                            }
                        ]
                    },
                    new Node3DAsset {
                        Name = "Camera",
                        Position = new Vector3(0f, 0f, 0f),
                        Features = [
                            new Camera3DAsset {
                                Id = cameraId,
                                RenderSettings = new() {
                                    Skybox = new CubemapAsset().WithImages(
                                        from path in new string[] {
                                            "posx", "negx",
                                            "posy", "negy",
                                            "posz", "negz"
                                        }
                                        select EmbeddedAssets.Load<HDRImageAsset>(
                                            "Nagule.Examples.Embedded.Textures.Skyboxes.Night." + path + ".hdr")
                                    )
                                }
                            },
                            new Light3DAsset {
                                Type = LightType.Point,
                                Range = 10f,
                                Color = new(1f, 1f, 1f, 5f)
                            }
                        ]
                    }
                ]
            };
            _sceneNode = Node3D.CreateEntity(world, scene);

            var torus = EmbeddedAssets.Load<Model3DAsset>("Nagule.Examples.Embedded.Models.torus.glb").RootNode;
            var sphere = EmbeddedAssets.Load<Model3DAsset>("Nagule.Examples.Embedded.Models.sphere.glb").RootNode;
            var vanilla = EmbeddedAssets.Load<Model3DAsset>("Nagule.Examples.Embedded.Models.vanilla_nekopara_fanart.glb").RootNode;

            var wallTex = new Texture2DAsset {
                Image = EmbeddedAssets.Load<ImageAsset>("Nagule.Examples.Embedded.Textures.wall.jpg"),
                Type = TextureType.Color
            };

            var wallMat = new MaterialAsset {
                Name = "Wall"
            }.WithProperties(
                new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.5f)),
                new(MaterialKeys.DiffuseTex, wallTex),
                new(MaterialKeys.Specular, new Vector4(0.3f)),
                new(MaterialKeys.Shininess, 32f));
            
            var transparentWallMat = wallMat with {
                RenderMode = RenderMode.Transparent
            };
            
            var wallTorus = new Node3DAsset {
                Features = [
                    (torus.Features.First() as Mesh3DAsset)! with { Material = transparentWallMat }
                ]
            };
            var wallSphere = new Node3DAsset {
                Features = [
                    (sphere.Features.First() as Mesh3DAsset)! with { Material = wallMat }
                ]
            };

            var tori = new Node3DAsset {
                Position = new Vector3(0, 0.2f, 0),
                Scale = new Vector3(0.3f)
            };
            _toriNode = Node3D.CreateEntity(world, tori);

            Node3D.CreateEntity(world, vanilla);

            for (int i = 0; i < 5000; ++i) {
                var entity = Node3D.CreateEntity(world, i % 2 == 0 ? wallTorus : wallSphere);
                entity.Modify(new Transform3D.SetParent(_toriNode));
                entity.Modify(new Transform3D.SetPosition(new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f)));
            }

            for (float y = 0; y < 10; ++y) {
                for (int i = 0; i < 200; ++i) {
                    int o = 50 + i * 2;
                    var light = Node3D.CreateEntity(world, new Node3DAsset {
                        Features = [
                            new Light3DAsset {
                                Type = LightType.Point,
                                Color = new Vector4(
                                    Random.Shared.NextSingle(),
                                    Random.Shared.NextSingle(),
                                    Random.Shared.NextSingle(), 10),
                                Range = 3f
                            }
                        ]
                    }, Tuple.Create(
                        new Rotator(-10 + Random.Shared.NextSingle() * 20)));

                    light.Modify(new Transform3D.SetPosition(
                        new Vector3(MathF.Sin(o) * o * 0.1f, y * 2, MathF.Cos(o) * o * 0.1f)));
                }
            }
        }

        public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        {
            var windowEntity = world.GetAddon<PrimaryWindow>().Entity;
            ref var mouse = ref windowEntity.Get<Mouse>();
            ref var keyboard = ref windowEntity.Get<Keyboard>();

            float deltaTime = _frame.DeltaTime;
            float scaledRate = deltaTime * _rate;

            if (_toriNode != default) {
                ref var toriTrans = ref _toriNode.Get<Transform3D>();

                _toriNode.Modify(ref toriTrans, new Transform3D.SetRotation(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)_frame.Time)));

                if (keyboard.IsKeyDown(Key.Space)) {
                    world.Destroy(_toriNode);
                    _toriNode = default;
                    return;
                }

                if (keyboard.IsKeyPressed(Key.Q)) {
                    _toriNode.Modify(ref toriTrans, new Transform3D.SetScale(
                        toriTrans.Scale - new Vector3(0.25f * deltaTime)));
                }
                if (keyboard.IsKeyPressed(Key.E)) {
                    _toriNode.Modify(ref toriTrans, new Transform3D.SetScale(
                        toriTrans.Scale + new Vector3(0.25f * deltaTime)));
                }
            }

            if (keyboard.IsKeyDown(Key.C)) {
                _controlActive = !_controlActive;
            }

            if (_controlActive) {
                ref var window = ref windowEntity.Get<Window>();
                var windowSize = new Vector2(window.Size.Item1, window.Size.Item2) / 2;
                _pos = Vector2.Lerp(_pos, (mouse.Position - windowSize) * _sensitivity, scaledRate);

                var camera = world.GetAddon<MainCamera3D>().Entity!.Value;
                var cameraNode = camera.Get<Feature>().Node;
                ref var cameraTrans = ref cameraNode.Get<Transform3D>();
                cameraNode.Modify(ref cameraTrans,
                    new Transform3D.SetRotation(Quaternion.CreateFromYawPitchRoll(-_pos.X, -_pos.Y, 0)));

                var direction = Vector3.Zero;
                bool movedThisFrame = false;

                if (keyboard.IsKeyPressed(Key.W)) {
                    direction += cameraTrans.WorldForward;
                    movedThisFrame = true;
                    _moving = true;
                }
                if (keyboard.IsKeyPressed(Key.S)) {
                    direction -= cameraTrans.WorldForward;
                    movedThisFrame = true;
                    _moving = true;
                }
                if (keyboard.IsKeyPressed(Key.A)) {
                    direction -= cameraTrans.WorldRight;
                    movedThisFrame = true;
                    _moving = true;
                }
                if (keyboard.IsKeyPressed(Key.D)) {
                    direction += cameraTrans.WorldRight;
                    movedThisFrame = true;
                    _moving = true;
                }
                if (_moving) {
                    _smoothDir = Vector3.Lerp(_smoothDir, direction, scaledRate);
                    if (!movedThisFrame && _smoothDir.Length() < 0.001f) {
                        _moving = false;
                        _smoothDir = Vector3.Zero;
                    }
                    else {
                        cameraNode.Modify(ref cameraTrans,
                            new Transform3D.SetPosition(cameraTrans.Position + _smoothDir * deltaTime * 5));
                    }
                }
            }
        }
    }

    public static void Run()
    {
        var world = new World();
        world.Start(() => {
            var frame = world.AcquireAddon<SimulationFrame>();

            SystemChain.Empty
                .Add<CoreModule>()
                .Add<GraphicsModule>()
                .Add<OpenTKGraphicsBackendModule>()
                .RegisterTo(world, frame.Scheduler);
            
            var window = world.CreateInBucketHost(Tuple.Create(
                new OpenTKWindow(),
                new PeripheralBundle {
                    Window = new Window {
                        IsFullscreen = false,
                        IsResizable = true
                    }
                },
                new SimulationContext(),
                new GraphicsContext {
                    RenderFrequency = 120
                }
            ));

            SystemChain.Empty
                .Add<RotatorSystem>()
                .Add<ExampleSystem>()
                .RegisterTo(world, frame.Scheduler);

            // world.Dispatcher.Listen(new EventListener());

            frame.Scheduler.Tick();
            window.Get<OpenTKWindow>().Native.Run();
        });
    }
}