namespace Nagule.Examples;

using System.Numerics;

using ImGuiNET;

using Aeco;

using Nagule;
using Nagule.Graphics;
using Nagule.Graphics.Backend.OpenTK;

public static class OpenTKExample
{
    public struct Rotator : Nagule.IPooledComponent
    {
    }

    private class LogicLayer : VirtualLayer, ILoadListener, IUnloadListener, IUpdateListener, IRenderListener
    {
        private float _rate = 10;
        private float _sensitivity = 0.005f;
        private float _x = 0;
        private float _y = 0;
        private Vector3 _deltaPos = Vector3.Zero;
        private bool _moving = false;

        private Guid _cameraId = Guid.NewGuid();
        private Guid _toriId = Guid.NewGuid();
        private Guid _lightsId = Guid.NewGuid();

        public void OnLoad(IContext context)
        {
            var game = (IContext)context;

            game.SetResource(_cameraId, new Camera {});
            game.Acquire<Transform>(_cameraId).Position = new Vector3(0, 0, 4f);
            game.Acquire<Parent>(_cameraId).Id = Graphics.RootId;

            var torusModel = InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.torus.glb");
            var sphereModel = InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.sphere.glb");

            var wallTexRes = new Texture {
                Image = InternalAssets.Load<Image>("Nagule.Examples.Embeded.Textures.wall.jpg"),
                Type = TextureType.Diffuse
            };

            var sphereMesh = sphereModel.RootNode!.Meshes![0] with {
                Material = new Material {
                    Name = "SphereMat",
                    Parameters = new() {
                        AmbientColor = new Vector4(0.2f),
                        DiffuseColor = new Vector4(1, 1, 1, 0.1f),
                        SpecularColor = new Vector4(0.3f),
                        Shininess = 32
                    }
                }.WithTexture(TextureType.Diffuse, wallTexRes)
            };

            var emissiveSphereMesh = sphereMesh with {
                Material = new Material {
                    Name = "EmissiveSphereMat",
                    Parameters = sphereMesh.Material.Parameters with {
                        EmissiveColor = new Vector4(0.8f, 1f, 0.8f, 2f),
                    }
                }
            };

            var torusMesh = torusModel.RootNode!.Meshes![0] with {
                Material = new Material {
                    Parameters = new() {
                        AmbientColor = new Vector4(1),
                        DiffuseColor = new Vector4(1, 1, 1, 1),
                        SpecularColor = new Vector4(0.3f),
                        Shininess = 32
                    }
                }
                .WithTexture(TextureType.Diffuse, wallTexRes)
            };

            var torusMeshTransparent = torusModel.RootNode!.Meshes![0] with {
                Material = new Material {
                    RenderMode = RenderMode.Transparent,
                    Parameters = new() {
                        AmbientColor = new Vector4(1),
                        DiffuseColor = new Vector4(1, 1, 1, 0.3f),
                        SpecularColor = new Vector4(0.5f),
                        Shininess = 32
                    }
                }
                .WithTexture(TextureType.Diffuse, wallTexRes)
            };

            var torusMeshCutoff = torusModel.RootNode!.Meshes![0] with {
                Material = new Material {
                    RenderMode = RenderMode.Cutoff,
                    Parameters = new() {
                        AmbientColor = new Vector4(1),
                        DiffuseColor = new Vector4(1, 1, 1, 0.3f),
                        SpecularColor = new Vector4(0.5f),
                        Shininess = 32
                    }
                }
                .WithTexture(TextureType.Diffuse, wallTexRes)
                .WithParameter("Threshold", 0.5f)
            };

            Guid CreateObject(Vector3 pos, Guid parentId, Mesh mesh)
            {
                var id = Guid.NewGuid();
                ref var renderable = ref game.Acquire<MeshRenderable>(id);
                renderable.Meshes.Add(mesh, MeshRenderMode.Instance);
                game.Acquire<Parent>(id).Id = parentId;
                game.Acquire<Transform>(id).Position = pos;
                return id;
            }

            Guid CreateLight(Vector3 pos, Guid parentId)
            {
                var id = Guid.NewGuid();
                game.SetResource(id, new Light {
                    Type = LightType.Point,
                    Color = new Vector4(
                        Random.Shared.NextSingle(),
                        Random.Shared.NextSingle(),
                        Random.Shared.NextSingle(), 10),
                    AttenuationQuadratic = 250f
                });
                game.Acquire<Parent>(id).Id = parentId;
                game.Acquire<Transform>(id).Position = pos;
                return id;
            }

            var sunId = Guid.NewGuid();

            game.CreateEntity().SetResource(
                new GraphNode {
                    Name = "Root"
                }
                .WithChild(
                    new GraphNode {
                        Name = "Sun",
                        Id = sunId,
                        Position = new Vector3(0, 1, 5),
                        Rotation = Quaternion.CreateFromYawPitchRoll(-90, -45, 0)
                    }
                    .WithLights(
                        new Light {
                            Type = LightType.Directional,
                            Color = new Vector4(1, 1, 1, 0.032f)
                        })));

            var cameraLightId = Guid.NewGuid();

            game.SetResource(cameraLightId,
                new GraphNode {
                    Name = "CameraLight",
                    Scale = new Vector3(0.05f),
                }
                .WithChild(
                    new GraphNode {
                        Name = "PointLight",
                        Position = new Vector3(0, 1, 0),
                    }
                    .WithLight(
                        new Light {
                            Type = LightType.Point,
                            Color = new Vector4(1, 1, 1, 5),
                            AttenuationQuadratic = 1f
                        })));

            game.Acquire<Parent>(cameraLightId).Id = _cameraId;

            var scene = game.CreateEntity();
            var sceneNode = InternalAssets.Load<Model>(
                "Nagule.Examples.Embeded.Models.library_earthquake.glb").RootNode;

            scene.SetResource(
                sceneNode.Recurse((rec, node) =>
                    node with {
                        Meshes = node.Meshes.ConvertAll(m => m with { IsOccluder = true }),
                        Children = node.Children.ConvertAll(rec)
                    }));

            game.CreateEntity().SetResource(
                InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.vanilla_nekopara_fanart.glb").RootNode);

            var x3dNode = InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.test.x3d").RootNode with {
                Position = new Vector3(0, -5, 0),
                Scale = new Vector3(0.5f)
            };
            game.CreateEntity().SetResource(x3dNode);

            ref var toriTrans = ref game.Acquire<Transform>(_toriId);
            toriTrans.LocalPosition = new Vector3(0, 0.2f, 0);
            toriTrans.LocalScale = new Vector3(0.3f);
            game.Acquire<Parent>(_toriId).Id = Graphics.RootId;
            //game.Acquire<Rotator>(_toriId);

            for (int i = 0; i < 5000; ++i) {
                var objId = CreateObject(new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f), _toriId,
                    i % 2 == 0 ? torusMesh : torusMeshTransparent);
                game.Acquire<Transform>(objId).LocalScale = new Vector3(0.9f);
            }

            game.Acquire<Rotator>(_lightsId);
            game.Acquire<Transform>(_lightsId).Position = new Vector3(0, 0.2f, 0);

            for (float y = 0; y < 10; ++y) {
                Guid groupId = Guid.NewGuid();
                game.Acquire<Parent>(groupId).Id = _lightsId;
                game.Acquire<Transform>(groupId).LocalAngles = new Vector3(0, Random.Shared.NextSingle() * 360, 0);
                for (int i = 0; i < 200; ++i) {
                    int o = 50 + i * 2;
                    var lightId = CreateLight(new Vector3(MathF.Sin(o) * o * 0.1f, y * 2, MathF.Cos(o) * o * 0.1f), groupId);
                    //game.Acquire<Rotator>(lightId);
                }
            }

            var spotLight = game.CreateEntity();
            spotLight.Acquire<Transform>().Position = new Vector3(0, 1, 0);
            spotLight.SetResource(new Light {
                Type = LightType.Spot,
                Color = new Vector4(0.5f, 1, 0.5f, 5),
                InnerConeAngle = 25,
                OuterConeAngle = 40,
                AttenuationQuadratic = 1
            });

            var pointLight = game.CreateEntity();
            pointLight.Acquire<Transform>().Position = new Vector3(0, 1, 0);
            pointLight.SetResource(new Light {
                Type = LightType.Point,
                Color = new Vector4(1, 1, 1, 1),
                AttenuationQuadratic = 0.7f
            });

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
                foreach (var (layer, profile) in profiles.OrderByDescending(v => v.Value.MaximumElapsedTime)) {
                    Console.WriteLine($"  {layer}: avg={profile.AverangeElapsedTime}, max={profile.MaximumElapsedTime}, min={profile.MinimumElapsedTime}");
                }
            }

            Console.WriteLine();

            var game = (IProfilingContext)context;
            PrintLayerProfiles("Update", game.GetProfiles<IUpdateListener>());
            PrintLayerProfiles("EngineUpdate", game.GetProfiles<IEngineUpdateListener>());
            PrintLayerProfiles("LateUpdate", game.GetProfiles<ILateUpdateListener>());
            PrintLayerProfiles("Render", game.GetProfiles<IRenderListener>());
        }

        float Lerp(float firstFloat, float secondFloat, float by)
            => firstFloat * (1 - by) + secondFloat * by;

        public void OnRender(IContext game)
        {
            ImGui.ShowDemoWindow();
        }

        public void OnUpdate(IContext game)
        {
            ref CameraRenderDebug GetDebug(IContext context)
                => ref context.Acquire<CameraRenderDebug>(_cameraId);
            
            ref readonly var window = ref game.InspectAny<Window>();
            ref readonly var mouse = ref game.InspectAny<Mouse>();
            ref readonly var keyboard = ref game.InspectAny<Keyboard>();

            float deltaTime = game.DeltaTime;
            float scaledRate = deltaTime * _rate;

            if (ImGui.IsKeyDown(ImGuiKey.Escape)) {
                game.Unload();
                return;
            }

            if (keyboard.States[Key.Space].Pressed && _toriId != Guid.Empty) {
                game.Destroy(_toriId);
                _toriId = Guid.Empty;
            }

            if (keyboard.States[Key.Q].Pressed) {
                game.Acquire<Transform>(_toriId).LocalScale += deltaTime * Vector3.One;
            }
            if (keyboard.States[Key.E].Pressed) {
                game.Acquire<Transform>(_toriId).LocalScale -= deltaTime * Vector3.One;
            }

            _x = Lerp(_x, (mouse.X - window.Width / 2) * _sensitivity, scaledRate);
            _y = Lerp(_y, (mouse.Y - window.Height / 2) * _sensitivity, scaledRate);

            foreach (var rotatorId in game.Query<Rotator>()) {
                ref var transform = ref game.Acquire<Transform>(rotatorId);
                transform.Position += transform.Forward * deltaTime * 2;
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, game.Time);
            }

            if (keyboard.States[Key.F1].Down) {
                game.RemoveAny<CameraRenderDebug>();
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
            Framerate = 60,
            IsFullscreen = true,
            IsResizable = false,
            VSyncMode = VSyncMode.Adaptive,
            //ClearColor = new Vector4(135f, 206f, 250f, 255f) / 255f
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