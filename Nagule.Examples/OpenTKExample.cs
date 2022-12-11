namespace Nagule.Examples;

using System.Numerics;

using Aeco;

using Nagule;
using Nagule.Graphics;
using Nagule.Backend.OpenTK;
using Nagule.Backend.OpenTK.Graphics;

public static class OpenTKExample
{
    public struct Rotator : Nagule.IPooledComponent
    {
    }

    private class LogicLayer : VirtualLayer, ILoadListener, IUnloadListener, IUpdateListener
    {
        private float _rate = 10;
        private float _sensitivity = 0.005f;
        private float _x = 0;
        private float _y = 0;
        private Vector3 _deltaPos = Vector3.Zero;
        private bool _moving = false;

        private Guid _cameraId = Guid.NewGuid();

        public void OnLoad(IContext context)
        {
            var game = (IEventContext)context;

            game.Acquire<Camera>(_cameraId);
            game.Acquire<Transform>(_cameraId).Position = new Vector3(0, 0, 4f);
            game.Acquire<Parent>(_cameraId).Id = Graphics.RootId;

            var torusModel = InternalAssets.Load<ModelResource>("Nagule.Examples.Embeded.Models.torus.glb");
            var sphereModel = InternalAssets.Load<ModelResource>("Nagule.Examples.Embeded.Models.sphere.glb");

            var wallTexRes = new TextureResource(
                InternalAssets.Load<ImageResource>("Nagule.Examples.Embeded.Textures.wall.jpg"));

            var sphereMesh = sphereModel.RootNode!.Meshes![0] with {
                Material = new MaterialResource {
                    Name = "SphereMat",
                    Parameters = new() {
                        AmbientColor = new Vector4(0.2f),
                        DiffuseColor = new Vector4(1, 1, 1, 1),
                        SpecularColor = new Vector4(0.3f),
                        Shininess = 32
                    }
                }.WithTexture(TextureType.Diffuse, wallTexRes)
            };

            var emissiveSphereMesh = sphereMesh with {
                Material = new MaterialResource {
                    Name = "EmissiveSphereMat",
                    Parameters = sphereMesh.Material.Parameters with {
                        EmissiveColor = new Vector4(0.8f, 1f, 0.8f, 2f),
                    }
                }
            };

            var torusMesh = torusModel.RootNode!.Meshes![0];
            torusMesh.Material = new MaterialResource {
                Parameters = new() {
                    AmbientColor = new Vector4(1),
                    DiffuseColor = new Vector4(1, 1, 1, 1),
                    SpecularColor = new Vector4(0.3f),
                    Shininess = 32
                }
            }.WithTexture(TextureType.Diffuse, wallTexRes);

            var torusMeshTransparent = torusModel.RootNode!.Meshes![0] with {
                Material = new MaterialResource {
                    IsTransparent = true,
                    Parameters = new() {
                        AmbientColor = new Vector4(1),
                        DiffuseColor = new Vector4(1, 1, 1, 0.3f),
                        SpecularColor = new Vector4(0.3f),
                        Shininess = 32
                    }
                }.WithTexture(TextureType.Diffuse, wallTexRes)
            };

            Guid CreateObject(Vector3 pos, Guid parentId, MeshResource mesh)
            {
                var id = Guid.NewGuid();
                ref var renderable = ref game.Acquire<MeshRenderable>(id);
                renderable.Meshes.Add(mesh, MeshRenderMode.Instance);
                renderable.Meshes.Add(sphereMesh, MeshRenderMode.Instance);
                game.Acquire<Parent>(id).Id = parentId;
                game.Acquire<Transform>(id).Position = pos;
                return id;
            }

            Guid CreateLight(Vector3 pos, Guid parentId)
            {
                var id = Guid.NewGuid();
                game.Acquire<Light>(id).Resource = new PointLightResource {
                    Color = new Vector4(1, 1, 1, 10),
                    AttenuationQuadratic = 150f
                };
                game.Acquire<Parent>(id).Id = parentId;
                game.Acquire<Transform>(id).Position = pos;
                return id;
            }

            var sunId = Guid.NewGuid();

            game.CreateEntity().Acquire<GraphNode>().Resource = new GraphNodeResource {
                Name = "Root",
                Children = new[] {
                    new GraphNodeResource {
                        Name = "Sun",
                        Id = sunId,
                        Position = new Vector3(0, 1, 5),
                        Rotation = Quaternion.CreateFromYawPitchRoll(-90, -45, 0),
                        Lights = new[] {
                            new DirectionalLightResource {
                                Color = new Vector4(1, 1, 1, 0.23f)
                            }
                        }
                    },
                }
            };

            var nodeId = Guid.NewGuid();
            game.Acquire<GraphNode>(nodeId).Resource = new GraphNodeResource {
                Name = "Sphere",
                Scale = new Vector3(0.5f),
                //Meshes = new[] { emissiveSphereMesh },
                Children = new[] {
                    new GraphNodeResource {
                        Name = "PointLight",
                        Position = new Vector3(0, 1, 0),
                        Lights = new[] {
                            new PointLightResource {
                                Color = new Vector4(1, 1, 1, 5),
                                AttenuationQuadratic = 1f
                            }
                        }
                    }
                }
            };
            game.Acquire<Parent>(nodeId).Id = _cameraId;

            game.CreateEntity().Acquire<GraphNode>().Resource =
                InternalAssets.Load<ModelResource>("Nagule.Examples.Embeded.Models.library_earthquake.glb").RootNode;

            game.CreateEntity().Acquire<GraphNode>().Resource =
                InternalAssets.Load<ModelResource>("Nagule.Examples.Embeded.Models.vanilla_nekopara_fanart.glb").RootNode;

/*
            var toriId = Guid.NewGuid();
            game.Acquire<Transform>(toriId).LocalScale = new Vector3(0.3f);
            game.Acquire<Parent>(toriId).Id = Graphics.RootId;

            for (int i = 0; i < 5000; ++i) {
                var objId = CreateObject(new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f), toriId,
                    i % 2 == 0 ? torusMesh : torusMeshTransparent);
                game.Acquire<Transform>(objId).LocalScale = new Vector3(0.99f);
            }

            Guid lightsId = Guid.NewGuid();
            game.Acquire<Rotator>(lightsId);
            game.Acquire<Transform>(lightsId).Position = new Vector3(0, 0, 0);

            for (int i = 0; i < 2000; ++i) {
                int o = 50 + i * 2;
                var lightId = CreateLight(new Vector3(MathF.Sin(o) * o * 0.1f, MathF.Cos(o) * o * 0.01f, MathF.Cos(o) * o * 0.1f), lightsId);
                //game.Acquire<Rotator>(lightId);
            }*/

            var spotLight = game.CreateEntity();
            spotLight.Acquire<Transform>().Position = new Vector3(0, 1, 0);
            spotLight.Acquire<Light>().Resource = new SpotLightResource {
                Color = new Vector4(0.5f, 1, 0.5f, 5),
                InnerConeAngle = 25,
                OuterConeAngle = 40,
                AttenuationQuadratic = 1
            };

            var pointLight = game.CreateEntity();
            pointLight.Acquire<Transform>().Position = new Vector3(0, 1, 0);
            pointLight.Acquire<Light>().Resource = new PointLightResource {
                Color = new Vector4(1, 1, 1, 1),
                AttenuationQuadratic = 0.7f
            };

            Guid rotatorId = CreateObject(Vector3.Zero, Graphics.RootId, emissiveSphereMesh);
            game.Acquire<Transform>(rotatorId).LocalScale = new Vector3(0.3f);
            game.Acquire<Rotator>(rotatorId);

            spotLight.Acquire<Parent>().Id = rotatorId;
            pointLight.Acquire<Parent>().Id = rotatorId;
        }

        public void OnUnload(IContext context)
        {
            void PrintLayerProfiles(string name, IReadOnlyDictionary<object, LayerProfile>? profiles)
            {
                Console.WriteLine($"[{name} Layer Profiles]");
                if (profiles == null) {
                    Console.WriteLine("  No layer.");
                    return;
                }
                foreach (var (layer, profile) in profiles.OrderByDescending(v => v.Value.AverangeElapsedTime)) {
                    Console.WriteLine($"  {layer}: avg={profile.AverangeElapsedTime}, max={profile.MaximumElapsedTime}, min={profile.MinimumElapsedTime}");
                }
            }

            Console.WriteLine();

            var game = (IProfilingEventContext)context;
            PrintLayerProfiles("Update", game.GetProfiles<IUpdateListener>());
            PrintLayerProfiles("EngineUpdate", game.GetProfiles<IEngineUpdateListener>());
            PrintLayerProfiles("LateUpdate", game.GetProfiles<ILateUpdateListener>());
            PrintLayerProfiles("Render", game.GetProfiles<IRenderListener>());
        }

        float Lerp(float firstFloat, float secondFloat, float by)
            => firstFloat * (1 - by) + secondFloat * by;

        public void OnUpdate(IContext game, float deltaTime)
        {
            ref RenderTargetDebug GetDebug(IContext context)
                => ref context.Acquire<RenderTargetDebug>(Graphics.DefaultRenderTargetId);
            
            ref readonly var window = ref game.InspectAny<Window>();
            ref readonly var mouse = ref game.InspectAny<Mouse>();
            ref readonly var keyboard = ref game.InspectAny<Keyboard>();

            if (keyboard.States[Key.Escape].Down) {
                game.Unload();
                return;
            }

            float scaledRate = deltaTime * _rate;

            _x = Lerp(_x, (mouse.X - window.Width / 2) * _sensitivity, scaledRate);
            _y = Lerp(_y, (mouse.Y - window.Height / 2) * _sensitivity, scaledRate);

            foreach (var rotatorId in game.Query<Rotator>()) {
                ref var transform = ref game.Acquire<Transform>(rotatorId);
                transform.Position += transform.Forward * deltaTime * 2;
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, game.Time);
            }

            if (keyboard.States[Key.F1].Down) {
                game.RemoveAny<RenderTargetDebug>();
            }
            if (keyboard.States[Key.F2].Down) {
                GetDebug(game).DisplayMode = DisplayMode.TransparencyAccum;
            }
            if (keyboard.States[Key.F3].Down) {
                GetDebug(game).DisplayMode = DisplayMode.TransparencyAlpha;
            }
            if (keyboard.States[Key.F4].Down) {
                GetDebug(game).DisplayMode = DisplayMode.Depth;
            }
            if (keyboard.States[Key.F5].Down) {
                GetDebug(game).DisplayMode = DisplayMode.Clusters;
            }

            ref var cameraTrans = ref game.Acquire<Transform>(_cameraId);
            cameraTrans.Rotation = Quaternion.CreateFromYawPitchRoll(-_x, -_y, 0);

            var deltaPos = Vector3.Zero;
            bool modified = false;

            if (keyboard.States[Key.W].Pressed) {
                deltaPos += cameraTrans.Forward;
                modified = true;
                _moving = true;
            }
            if (keyboard.States[Key.S].Pressed) {
                deltaPos -= cameraTrans.Forward;
                modified = true;
                _moving = true;
            }
            if (keyboard.States[Key.A].Pressed) {
                deltaPos -= cameraTrans.Right;
                modified = true;
                _moving = true;
            }
            if (keyboard.States[Key.D].Pressed) {
                deltaPos += cameraTrans.Right;
                modified = true;
                _moving = true;
            }
            if (_moving) {
                _deltaPos = Vector3.Lerp(_deltaPos, deltaPos, scaledRate);
                if (!modified && _deltaPos.Length() < 0.001f) {
                    _moving = false;
                    _deltaPos = Vector3.Zero;
                }
                else {
                    cameraTrans.Position += _deltaPos * deltaTime * 5;
                }
            }
        }
    }

    public static void Run()
    {
        var window = new OpenTKWindow(new GraphicsSpecification {
            Width = 1920 / 2,
            Height = 1080 / 2,
            RenderFrequency = 60,
            UpdateFrequency = 60,
            IsFullscreen = true,
            IsResizable = false,
            Title = "RPG Game",
            //IsDebugEnabled = true
        });

        var game = new ProfilingEventContext(
            window,
            new LogicLayer(),
            new OpenTKGraphics()
        );

        game.Load();
        window.Run();
    }
}