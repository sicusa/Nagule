namespace Nagule.Examples;

using System.Numerics;

using Sia;

using Nagule;
using Nagule.Prelude;
using Nagule.Graphics;
using Nagule.Graphics.UI;
using Nagule.Graphics.PostProcessing;
using Nagule.Graphics.Backend.OpenTK;

public static class Example
{
    private static RNode3D CreateSceneNode()
    {
        var occluderOptions = new Model3DLoadOptions {
            IsOccluder = true
        };

        var wallMat = new RMaterial {
            Name = "Wall"
        }.WithProperties(
            new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.5f)),
            new(MaterialKeys.DiffuseTex, new RTexture2D {
                Image = EmbeddedAssets.LoadInternal<RImage>("textures.wall.jpg"),
                Type = TextureType.Color
            }),
            new(MaterialKeys.Specular, new Vector4(0.3f)),
            new(MaterialKeys.Shininess, 32f));
        
        var transparentWallMat = wallMat with {
            RenderMode = RenderMode.Transparent
        };
        
        var torus = EmbeddedAssets.LoadInternal<RModel3D>("models.torus.glb").RootNode;
        var sphere = EmbeddedAssets.LoadInternal<RModel3D>("models.sphere.glb").RootNode;

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

        static RUpdator CreateRotationFeature(float speed)
            => new((node, frame) => {
                node.SetRotation(
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, speed * frame.Time));
            });
        
        static RUpdator CreateMoverFeature(float speed)
            => new((node, frame) => {
                ref var transform = ref node.Get<Transform3D>();
                node.SetPosition(transform.Position + transform.Forward * frame.DeltaTime * speed);
            });

        var dynamicLights = Enumerable.Range(0, 2000).Select(i => {
            int y = i / 200;
            int c = i % 200;
            int o = 50 + c * 2;

            return new RNode3D {
                Position = new(MathF.Sin(o) * o * 0.1f, y * 2, MathF.Cos(o) * o * 0.1f),
                Features = [
                    new RLight3D {
                        Type = LightType.Point,
                        Color = new Vector4(
                            Random.Shared.NextSingle(),
                            Random.Shared.NextSingle(),
                            Random.Shared.NextSingle(), 10),
                        Range = 3f
                    },
                    CreateMoverFeature(-10 + Random.Shared.NextSingle() * 20),
                    CreateRotationFeature(1f)
                ]
            };
        });

        return new RNode3D {
            Children = [
                EmbeddedAssets.LoadInternal(
                    AssetPath<RModel3D>.From("models.abandoned_warehouse.glb"), occluderOptions).RootNode with {
                    Position = new(0, 0, 0)
                },

                EmbeddedAssets.LoadInternal<RModel3D>(
                    "models.vanilla_nekopara_fanart.glb").RootNode,

                new RNode3D {
                    Name = "Lighting",
                    Features = [
                        new RLight3D {
                            Type = LightType.Ambient,
                            Color = new(1f, 1f, 1f, 0.2f)
                        }
                    ],
                    Children = [
                        new RNode3D {
                            Name = "Sun",
                            Rotation = new(-45f, -45f, 0f),
                            Features = [
                                new RLight3D {
                                    Type = LightType.Directional,
                                    Color = new(1f, 1f, 1f, 0.032f)
                                }
                            ]
                        },
                        new RNode3D {
                            Name = "Dynamic Lights",
                            Features = [
                                //new RGenerator3D(dynamicLights)
                            ]
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
                                        "textures.skyboxes.night." + path + ".hdr")
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
                        },
                        new RFirstPersonController()
                    ]
                },
                new RNode3D {
                    Name = "Tori",
                    Position = new Vector3(0, 0.2f, 0),
                    Scale = new Vector3(0.3f),
                    Features = [
                        new REvents {
                            Start = (world, node) => {
                                for (int i = 0; i < 5000; ++i) {
                                    var entity = Node3D.CreateEntity(
                                        world, i % 2 == 0 ? wallTorus : wallSphere, node);
                                    entity.SetPosition(
                                        new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f));
                                }
                            }
                        },
                        new RUpdator((world, node, frame) => {
                            var peri = world.GetAddon<Peripheral>();
                            ref var keyboard = ref peri.Keyboard;
                            ref var transform = ref node.Get<Transform3D>();

                            if (keyboard.IsKeyDown(Key.Space)) {
                                node.Dispose();
                                return;
                            }
                            if (keyboard.IsKeyPressed(Key.Q)) {
                                node.SetScale(transform.Scale - new Vector3(0.25f * frame.DeltaTime));
                            }
                            if (keyboard.IsKeyPressed(Key.E)) {
                                node.SetScale(transform.Scale + new Vector3(0.25f * frame.DeltaTime));
                            }
                        }),
                        CreateRotationFeature(1f)
                    ]
                }
            ]
        };
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
                .Add<PreludeModule>()
                .Add<OpenTKGraphicsBackendModule>()
                .RegisterTo(world, frame.Scheduler);
            
            var window = world.CreateInBucketHost(Tuple.Create(
                new OpenTKWindow(),
                new Window {
                    IsFullscreen = true
                },
                new PeripheralBundle(),
                new SimulationContext(),
                new GraphicsContext {
                    RenderFrequency = 120
                }
            ));

            Node3D.CreateEntity(world, CreateSceneNode());

            frame.Scheduler.Tick();
            window.Get<OpenTKWindow>().Native.Run();
        });
    }
}