namespace Nagule.Examples;

using System.Numerics;

using ImGuiNET;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;
using Nagule.Graphics.Backend.OpenTK;

public static class OpenTKExample
{
    public struct Rotator : IReactiveComponent
    {
        public float Speed = 2f;

        public Rotator() {}
    }

    private class LogicLayer : Layer, ILoadListener, IUnloadListener, IUpdateListener
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

        private Group<Rotator> _rotators = new();

        public void OnLoad(IContext context)
        {
            context.SetResource(_cameraId, new Camera {
                RenderSettings = new RenderSettings {
                    Skybox = new Cubemap().WithImages(
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.px.hdr"),
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.nx.hdr"),
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.ny.hdr"),
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.py.hdr"),
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.pz.hdr"),
                        InternalAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.nz.hdr"))
                }
            });

            context.Acquire<Transform>(_cameraId).Position = new Vector3(0, 0, 4f);
            context.Acquire<Parent>(_cameraId).Id = Graphics.RootId;

            var torusModel = InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.torus.glb");
            var sphereModel = InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.sphere.glb");

            var wallTex = new Texture {
                Image = InternalAssets.Load<Image>("Nagule.Examples.Embeded.Textures.wall.jpg"),
                Type = TextureType.Diffuse
            };

            var sphereMesh = sphereModel.RootNode!.MeshRenderable!.Meshes.First() with {
                Material = new Material { Name = "SphereMat" }
                    .WithProperties(
                        new(MaterialKeys.Ambient, new Vector4(0.2f)),
                        new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.1f)),
                        new(MaterialKeys.DiffuseTex, wallTex),
                        new(MaterialKeys.Specular, new Vector4(0.3f)),
                        new(MaterialKeys.Shininess, 32f))
            };

            var emissiveSphereMesh = sphereMesh with {
                Material = new Material {
                    Name = "EmissiveSphereMat",
                    Properties = sphereMesh.Material.Properties.SetItem(
                        MaterialKeys.Emission, Dyn.From(0.8f, 1f, 0.8f, 2f))
                }
            };

            var torusMesh = torusModel.RootNode!.MeshRenderable!.Meshes.First() with {
                Material = new Material()
                    .WithProperties(
                        new(MaterialKeys.Ambient, new Vector4(1f)),
                        new(MaterialKeys.Diffuse, new Vector4(1f)),
                        new(MaterialKeys.DiffuseTex, wallTex),
                        new(MaterialKeys.Specular, new Vector4(0.3f)),
                        new(MaterialKeys.Shininess, 32f))
            };

            var torusMeshTransparent = torusMesh with {
                Material =
                    new Material {
                        RenderMode = RenderMode.Transparent,
                    }
                    .WithProperties(
                        new(MaterialKeys.Ambient, new Vector4(1f)),
                        new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.3f)),
                        new(MaterialKeys.DiffuseTex, wallTex),
                        new(MaterialKeys.Specular, new Vector4(0.5f)),
                        new(MaterialKeys.Shininess, 32f))
            };

            var torusMeshCutoff = torusMesh with {
                Material =
                    new Material {
                        RenderMode = RenderMode.Cutoff,
                    }
                    .WithProperties(
                        new(MaterialKeys.Ambient, new Vector4(1f)),
                        new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.3f)),
                        new(MaterialKeys.DiffuseTex, wallTex),
                        new(MaterialKeys.Specular, new Vector4(0.5f)),
                        new(MaterialKeys.Shininess, 32f),
                        new(MaterialKeys.Threshold, 0.5f))
            };

            Guid CreateObject(Vector3 pos, Guid parentId, Mesh mesh)
            {
                var id = Guid.NewGuid();
                context.SetResource(id,
                    MeshRenderable.Empty.WithMesh(mesh));
                context.Acquire<Parent>(id).Id = parentId;
                context.Acquire<Transform>(id).Position = pos;
                return id;
            }

            Guid CreateLight(Vector3 pos, Guid parentId)
            {
                var id = Guid.NewGuid();
                context.SetResource(id, new Light {
                    Type = LightType.Point,
                    Color = new Vector4(
                        Random.Shared.NextSingle(),
                        Random.Shared.NextSingle(),
                        Random.Shared.NextSingle(), 5),
                    Range = 3f
                });
                context.Acquire<Parent>(id).Id = parentId;
                context.Acquire<Transform>(id).Position = pos;
                return id;
            }

            var sunId = Guid.NewGuid();

            context.SetResource(Guid.NewGuid(),
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

            context.SetResource(cameraLightId,
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
                            Range = 10f
                        })));

            context.Acquire<Parent>(cameraLightId).Id = _cameraId;

            var sceneNode = InternalAssets.Load<Model>(
                "Nagule.Examples.Embeded.Models.library_earthquake.glb").RootNode;
            context.SetResource(Guid.NewGuid(), sceneNode.MakeOccluder());

            context.SetResource(Guid.NewGuid(),
                InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.vanilla_nekopara_fanart.glb").RootNode);
            
            var heightTex = new Texture {
                Image = InternalAssets.Load<Image>("Nagule.Examples.Embeded.Textures.iceland_heightmap.png"),
                Type = TextureType.Height
            };

            var planeNode = InternalAssets.Load<Model>(
                "Nagule.Examples.Embeded.Models.plane.glb").RootNode;

            context.SetResource(Guid.NewGuid(), planeNode with {
                Position = new Vector3(0, 0.5f, 0),
                Scale = new Vector3(1.5f),
                MeshRenderable = planeNode.MeshRenderable!.ConvertMeshes(
                    mesh => mesh with {
                        Material = mesh.Material
                            .WithProperties(
                                new(MaterialKeys.Diffuse, new Vector4(1f)),
                                new(MaterialKeys.DiffuseTex, heightTex),
                                new(MaterialKeys.HeightTex, heightTex),
                                new(MaterialKeys.ParallaxScale, 0.1f),
                                new(MaterialKeys.EnableParallaxOversampledUVClip))
                    })
            });

            /*game.SetResource(Guid.NewGuid(),
                InternalAssets.Load<Model>("Nagule.Examples.Embeded.Models.test.x3d").RootNode with {
                    Position = new Vector3(0, -5, 0),
                    Scale = new Vector3(0.5f)
                });*/

/*
            ref var toriTrans = ref context.Acquire<Transform>(_toriId);
            toriTrans.LocalPosition = new Vector3(0, 0.2f, 0);
            toriTrans.LocalScale = new Vector3(0.3f);
            context.Acquire<Parent>(_toriId).Id = Graphics.RootId;
            context.Acquire<Rotator>(_toriId);

            for (int i = 0; i < 5000; ++i) {
                var objId = CreateObject(new Vector3(MathF.Sin(i) * i * 0.1f, 0, MathF.Cos(i) * i * 0.1f), _toriId,
                    i % 2 == 0 ? torusMesh : torusMeshTransparent);
                context.Acquire<Transform>(objId).LocalScale = new Vector3(0.9f);
            }*/

            context.Acquire<Transform>(_lightsId).Position = new Vector3(0, 0.2f, 0);

            for (float y = 0; y < 10; ++y) {
                Guid groupId = Guid.NewGuid();
                context.Acquire<Parent>(groupId).Id = _lightsId;
                context.Acquire<Transform>(groupId).LocalAngles = new Vector3(0, Random.Shared.NextSingle() * 360, 0);
                for (int i = 0; i < 200; ++i) {
                    int o = 50 + i * 2;
                    var lightId = CreateLight(new Vector3(MathF.Sin(o) * o * 0.1f, y * 2, MathF.Cos(o) * o * 0.1f), groupId);
                    context.Acquire<Rotator>(lightId).Speed = -10 + Random.Shared.NextSingle() * 20;
                }
            }

            var spotLightId = Guid.NewGuid();
            context.Acquire<Transform>(spotLightId).Position = new Vector3(0, 1, 0);
            context.SetResource(spotLightId, new Light {
                Type = LightType.Spot,
                Color = new Vector4(0.5f, 1, 0.5f, 5),
                InnerConeAngle = 25,
                OuterConeAngle = 40,
                Range = 5f
            });

            var pointLightId = Guid.NewGuid();
            context.Acquire<Transform>(pointLightId).Position = new Vector3(0, 1, 0);
            context.SetResource(pointLightId, new Light {
                Type = LightType.Point,
                Color = new Vector4(1, 1, 1, 1),
                Range = 1f
            });

            Guid rotatorId = CreateObject(Vector3.Zero, Graphics.RootId, emissiveSphereMesh);
            context.Acquire<Transform>(rotatorId).LocalScale = new Vector3(0.3f);
            context.Acquire<Rotator>(rotatorId);

            context.Acquire<Parent>(spotLightId).Id = rotatorId;
            context.Acquire<Parent>(pointLightId).Id = rotatorId;
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

            var game = (IProfilingContext)context;
            PrintLayerProfiles("OnFrameStart", game.GetProfiles<IFrameStartListener>());
            PrintLayerProfiles("Update", game.GetProfiles<IUpdateListener>());
            PrintLayerProfiles("EngineUpdate", game.GetProfiles<IEngineUpdateListener>());
            PrintLayerProfiles("LateUpdate", game.GetProfiles<ILateUpdateListener>());
            PrintLayerProfiles("Render", game.GetProfiles<IRenderListener>());
        }

        float Lerp(float firstFloat, float secondFloat, float by)
            => firstFloat * (1 - by) + secondFloat * by;

        public void OnUpdate(IContext context)
        {
            ImGui.ShowDemoWindow();

            ref CameraRenderDebug GetDebug(IContext context)
                => ref context.Acquire<CameraRenderDebug>(_cameraId);
            
            ref readonly var window = ref context.InspectAny<Window>();
            ref readonly var mouse = ref context.InspectAny<Mouse>();
            ref readonly var keyboard = ref context.InspectAny<Keyboard>();

            float deltaTime = context.DeltaTime;
            float scaledRate = deltaTime * _rate;

            if (ImGui.IsKeyDown(ImGuiKey.Escape)) {
                context.Unload();
                return;
            }

            if (keyboard.States[Key.Space].Pressed && _lightsId != Guid.Empty) {
                context.Destroy(_lightsId);
                context.Destroy(_toriId);
                _lightsId = Guid.Empty;
                _toriId = Guid.Empty;
            }

            if (keyboard.States[Key.Q].Pressed) {
                context.Acquire<Transform>(_toriId).LocalScale += deltaTime * Vector3.One;
            }
            if (keyboard.States[Key.E].Pressed) {
                context.Acquire<Transform>(_toriId).LocalScale -= deltaTime * Vector3.One;
            }

            _x = Lerp(_x, (mouse.X - window.Width / 2) * _sensitivity, scaledRate);
            _y = Lerp(_y, (mouse.Y - window.Height / 2) * _sensitivity, scaledRate);

            foreach (var rotatorId in _rotators.Query(context)) {
                ref var transform = ref context.Acquire<Transform>(rotatorId);
                transform.Position += transform.Forward * deltaTime * context.Inspect<Rotator>(rotatorId).Speed;
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, context.Time);
            }

            if (keyboard.States[Key.F1].Down) {
                context.RemoveAny<CameraRenderDebug>();
            }
            if (keyboard.States[Key.F2].Down) {
                GetDebug(context).DisplayMode = DisplayMode.TransparencyAccum;
            }
            if (keyboard.States[Key.F3].Down) {
                GetDebug(context).DisplayMode = DisplayMode.TransparencyAlpha;
            }
            if (keyboard.States[Key.F4].Down) {
                GetDebug(context).DisplayMode = DisplayMode.Depth;
            }
            if (keyboard.States[Key.F5].Down) {
                GetDebug(context).DisplayMode = DisplayMode.Clusters;
            }

            ref var cameraTrans = ref context.Acquire<Transform>(_cameraId);
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
            IsFullscreen = true,
            IsResizable = false,
            VSyncMode = VSyncMode.Adaptive
            //ClearColor = new Vector4(135f, 206f, 250f, 255f) / 255f
        });
        
        var game = new ProfilingContext(
            window,
            new LogicLayer(),
            new OpenTKGraphics()
        );

        game.Load();
        window.Run();
    }
}