namespace Nagule.Examples;

using System.Numerics;

using Sia;

using Nagule;
using Nagule.Prelude;
using Nagule.Graphics;
using Nagule.Graphics.UI;
using Nagule.Graphics.PostProcessing;
using Nagule.Graphics.Backends.OpenTK;
using Nagule.Reactive;
using System.Reactive.Linq;

public static class Example
{
    private static RNode3D CreateSceneNode()
    {
        var occluderOptions = new Model3DLoadOptions {
            IsOccluder = true
        };

        var wallTex = new RTexture2D {
            Image = EmbeddedAssets.LoadInternal<RImage>("textures.wall.jpg"),
            Usage = TextureUsage.Color
        };

        var wallMat = new RMaterial {
            Name = "wall"
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
                EmbeddedMeshes.Torus with {
                    Material = transparentWallMat
                }
            ]
        };

        var wallSphere = new RNode3D {
            Features = [
                EmbeddedMeshes.Sphere with {
                    Material = wallMat
                }
            ]
        };

        static RUpdator CreateRotationFeature(float speed, Vector3 axis)
        {
            axis = Vector3.Normalize(axis);
            return new((node, framer) => {
                node.SetRotation(
                    Quaternion.CreateFromAxisAngle(axis, speed * framer.Time));
            });
        }
        
        static RUpdator CreateMoverFeature(float speed)
            => new((node, framer) => {
                ref var transform = ref node.Get<Transform3D>();
                node.SetPosition(transform.Position + transform.Forward * framer.DeltaTime * speed);
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
                    CreateRotationFeature(1f, Vector3.UnitY)
                ]
            };
        });

        return new RNode3D {
            Children = [
                EmbeddedAssets.LoadInternal(
                    AssetPath<RModel3D>.From("models.library_earthquake.glb"), occluderOptions).RootNode with {
                    Position = new(0, 0, 0)
                },

                EmbeddedAssets.LoadInternal<RModel3D>(
                    "models.vanilla_nekopara_fanart.glb").RootNode,
                
                new RNode3D {
                    Name = "Test",
                    Position = new(0, 5, 0),
                    Features = [
                        EmbeddedMeshes.Plane with {
                            Material = new RMaterial {
                                Name = "test_material",
                                ShaderProgram = new RGLSLProgram { Name = "test_shader" }
                                    .WithShaders(
                                        new(ShaderType.Vertex,
                                            EmbeddedAssets.LoadInternal<RText>("shaders.test.vert.glsl")),
                                        new(ShaderType.Fragment,
                                            EmbeddedAssets.LoadInternal<RText>("shaders.test.frag.glsl")))
                                    .WithParameters(
                                        new(MaterialKeys.DiffuseTex),
                                        new(new TypedKey<RArrayTexture2D>("TestArrayTex")),
                                        new(new TypedKey<RTileset2D>("TestTilesetTex")))
                            }.WithProperties(
                                new(MaterialKeys.DiffuseTex, new RTexture2D {
                                    Image = EmbeddedAssets.LoadInternal<RImage>("textures.phoebus.png"),
                                    Usage = TextureUsage.Color
                                }),
                                new("TestArrayTex", new RArrayTexture2D {
                                    Images = [
                                        RImage.Hint,
                                        RImage.White
                                    ]
                                }),
                                new("TestTilesetTex", new RTileset2D {
                                    Image = EmbeddedAssets.LoadInternal<RImage>("textures.phoebus.png"),
                                    TileWidth = 64,
                                    TileHeight = 64
                                })
                            )
                        }
                    ]
                },

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
                            Rotation = new(-45f, -45f, 0f),
                            Features = [
                                new RLight3D {
                                    Name = "Sun",
                                    Type = LightType.Directional,
                                    Color = new(1f, 1f, 1f, 0.032f),
                                    IsShadowEnabled = true
                                },
                                CreateRotationFeature(0.1f, Vector3.One)
                            ]
                        },
                        new RNode3D {
                            Name = "Dynamic Lights",
                            Features = [
                                new RGenerator3D(dynamicLights)
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
                                SunLight = "Sun"
                            }
                        },
                        new REffectEnvironment {
                            Pipeline = new() {
                                Effects = [
                                    new RProcedualSkybox(),
                                    new RACESToneMapping(),
                                    new RGammaCorrection()
                                ]
                            }
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

                                NaObservables.FromEvent<Keyboard.OnKeyStateChanged>()
                                    .Where(e => e.Event.IsKeyPressed(Key.T))
                                    .TakeUntilDestroy(node)
                                    .ThrottleFirst(1f)
                                    .Do(_ => Console.WriteLine("Pressed!"))
                                    .Subscribe();

                                NaObservables.FromEvent<Keyboard.OnKeyStateChanged>()
                                    .Where(e => e.Event.IsKeyPressed(Key.Space))
                                    .TakeUntilDestroy(node)
                                    .Do(_ => node.Dispose())
                                    .Take(1)
                                    .Subscribe();
                            }
                        },
                        new RUpdator((world, node, framer) => {
                            var peri = world.GetAddon<Peripheral>();
                            ref var keyboard = ref peri.Keyboard;
                            ref var transform = ref node.Get<Transform3D>();

                            if (keyboard.IsKeyPressed(Key.Q)) {
                                node.SetScale(transform.Scale - new Vector3(0.25f * framer.DeltaTime));
                            }
                            if (keyboard.IsKeyPressed(Key.E)) {
                                node.SetScale(transform.Scale + new Vector3(0.25f * framer.DeltaTime));
                            }
                        }),
                        CreateRotationFeature(1f, Vector3.UnitY)
                    ]
                }
            ]
        };
    }

    public static void Run()
    {
        var world = new World();
        world.Start(() => {
            var framer = world.AcquireAddon<SimulationFramer>();

            SystemChain.Empty
                .Add<CoreModule>()
                .Add<GraphicsModule>()
                .Add<UIModule>()
                .Add<PostProcessingModule>()
                .Add<OpenTKGraphicsBackendsModule>()
                .Add<PreludeModule>()
                .RegisterTo(world, framer.Scheduler);
            
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

            framer.Scheduler.Tick();
            window.Get<OpenTKWindow>().Native.Run();
        });
    }
}