namespace Nagule.Examples;

using Sia;

using Nagule;
using Nagule.Graphics;
using Nagule.Graphics.Backend.OpenTK;
using System.Numerics;
using Nagule.Graphics.PostProcessing;
using Nagule.Graphics.UI;

public static class Example
{
    public record struct Rotator(float Speed);

    private class RotatorSystem()
        : SystemBase(
            matcher: Matchers.Of<Transform3D, Rotator>())
    {
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

    private class ExampleSystem()
        : SystemBase(matcher: Matchers.Any)
    {
        private float _rate = 10;
        private float _sensitivity = 0.005f;
        private Vector2 _pos = Vector2.Zero;
        private Vector3 _smoothDir = Vector3.Zero;
        private bool _moving = false;
        private bool _controlActive = true;

        private EntityRef _sceneNode;
        private EntityRef _toriNode;

        public override void Initialize(World world, Scheduler scheduler)
        {
            base.Initialize(world, scheduler);

            var occluderOptions = new Model3DLoadOptions {
                IsOccluder = true
            };

            var scene = new RNode3D {
                Children = [
                    EmbeddedAssets.LoadInternal(
                        AssetPath<RModel3D>.From("Models.abandoned_warehouse.glb"), occluderOptions).RootNode,

                    EmbeddedAssets.LoadInternal<RModel3D>(
                        "Models.vanilla_nekopara_fanart.glb").RootNode,

                    new RNode3D {
                        Name = "Lighting",
                        Rotation = new(-45f, -45f, 0f),
                        Features = [
                            new RLight3D {
                                Type = LightType.Directional,
                                Color = new(1f, 1f, 1f, 0.032f)
                            },
                            new RLight3D {
                                Type = LightType.Ambient,
                                Color = new(1f, 1f, 1f, 0.2f)
                            }
                        ]
                    },
                    new RNode3D {
                        Name = "Camera",
                        Position = new(0f, 0f, 0f),
                        Features = [
                            new RCamera3D {
                                RenderSettings = new() {
                                    Skybox = new RCubemap().WithImages(
                                        from path in new string[] {
                                            "posx", "negx",
                                            "posy", "negy",
                                            "posz", "negz"
                                        }
                                        select EmbeddedAssets.LoadInternal<RHDRImage>(
                                            "Textures.Skyboxes.Night." + path + ".hdr")
                                    )
                                }
                            },
                            new REffectEnvironment {
                                Pipeline = new REffectPipeline {
                                    Effects = [
                                        new RACESToneMapping(),
                                        new RGammaCorrection()
                                    ]
                                }
                            },
                            new RLight3D {
                                Type = LightType.Point,
                                Range = 10f,
                                Color = new(1f, 1f, 1f, 5f)
                            }
                        ]
                    }
                ]
            };
            _sceneNode = Node3D.CreateEntity(world, scene);

            var torus = EmbeddedAssets.LoadInternal<RModel3D>("Models.torus.glb").RootNode;
            var sphere = EmbeddedAssets.LoadInternal<RModel3D>("Models.sphere.glb").RootNode;

            var wallTex = new RTexture2D {
                Image = EmbeddedAssets.LoadInternal<RImage>("Textures.wall.jpg"),
                Type = TextureType.Color
            };

            var wallMat = new RMaterial {
                Name = "Wall"
            }.WithProperties(
                new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.5f)),
                new(MaterialKeys.DiffuseTex, wallTex),
                new(MaterialKeys.Specular, new Vector4(0.3f)),
                new(MaterialKeys.Shininess, 32f));
            
            var transparentWallMat = wallMat with {
                RenderMode = RenderMode.Transparent
            };
            
            var wallTorus = new RNode3D {
                Features = [
                    (torus.Features.First() as RMesh3D)! with { Material = transparentWallMat }
                ]
            };
            var wallSphere = new RNode3D {
                Features = [
                    (sphere.Features.First() as RMesh3D)! with { Material = wallMat }
                ]
            };

            var tori = new RNode3D {
                Position = new Vector3(0, 0.2f, 0),
                Scale = new Vector3(0.3f)
            };
            _toriNode = Node3D.CreateEntity(world, tori);

            for (int i = 0; i < 5000; ++i) {
                var entity = Node3D.CreateEntity(world, i % 2 == 0 ? wallTorus : wallSphere);
                entity.Modify(new Transform3D.SetParent(_toriNode));
                entity.Modify(new Transform3D.SetPosition(
                    new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f)));
            }

            for (float y = 0; y < 0; ++y) {
                for (int i = 0; i < 200; ++i) {
                    int o = 50 + i * 2;
                    var light = Node3D.CreateEntity(world, new RNode3D {
                        Features = [
                            new RLight3D {
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
            var frame = world.GetAddon<SimulationFrame>();
            var windowEntity = world.GetAddon<PrimaryWindow>().Entity;
            ref var mouse = ref windowEntity.Get<Mouse>();
            ref var keyboard = ref windowEntity.Get<Keyboard>();

            float deltaTime = frame.DeltaTime;
            float scaledRate = deltaTime * _rate;

            if (_toriNode != default) {
                ref var toriTrans = ref _toriNode.Get<Transform3D>();

                _toriNode.Modify(ref toriTrans, new Transform3D.SetRotation(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)frame.Time)));

                if (keyboard.IsKeyDown(Key.Space)) {
                    _toriNode.Destroy();
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

                var camera = world.GetAddon<MainCamera3D>().Entity;
                var cameraNode = camera.GetFeatureNode();

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
                .Add<UIModule>()
                .Add<PostProcessingModule>()
                .Add<OpenTKGraphicsBackendModule>()
                .RegisterTo(world, frame.Scheduler);
            
            var window = world.CreateInBucketHost(Tuple.Create(
                new OpenTKWindow(),
                new PeripheralBundle {
                    Window = new Window {
                        IsFullscreen = true
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