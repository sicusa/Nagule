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
        private Guid _sunId = Guid.NewGuid();
        private Guid _cameraId = Guid.NewGuid();
        private Guid _toriId = Guid.NewGuid();
        private Guid _lightsId = Guid.NewGuid();

        private Group<Rotator> _rotators = new();

        public void OnLoad(IContext context)
        {
            context.SetResource(_cameraId, new Camera {
                RenderSettings = new RenderSettings {
                    Skybox = new Cubemap().WithImages(
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.px.hdr"),
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.nx.hdr"),
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.ny.hdr"),
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.py.hdr"),
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.pz.hdr"),
                        EmbededAssets.Load<Image<float>>("Nagule.Examples.Embeded.Textures.Skyboxes.nz.hdr"))
                }
            });

            context.Acquire<Transform>(_cameraId).Position = new Vector3(0, 0, 4f);
            context.Acquire<Parent>(_cameraId).Id = Graphics.RootId;

            var torusModel = EmbededAssets.Load<Model>("Nagule.Examples.Embeded.Models.torus.glb");
            var sphereModel = EmbededAssets.Load<Model>("Nagule.Examples.Embeded.Models.sphere.glb");

            var wallTex = new Texture {
                Image = EmbededAssets.Load<Image>("Nagule.Examples.Embeded.Textures.wall.jpg"),
                Type = TextureType.Color
            };

            var sphereMesh = sphereModel.RootNode!.MeshRenderable!.Meshes.First() with {
                Material = new Material { Name = "SphereMat" }
                    .WithProperties(
                        new(MaterialKeys.Diffuse, new Vector4(1, 1, 1, 0.1f)),
                        new(MaterialKeys.DiffuseTex, wallTex),
                        new(MaterialKeys.Specular, new Vector4(0.3f)),
                        new(MaterialKeys.Shininess, 32f))
            };

            var emissiveSphereMesh = sphereMesh with {
                Material =
                    new Material {
                        Name = "EmissiveSphereMat",
                    }
                    .WithProperty(new(MaterialKeys.Emission, new Vector4(0.8f, 1f, 0.8f, 2f)))
            };

            var torusMesh = torusModel.RootNode!.MeshRenderable!.Meshes.First() with {
                Material = new Material()
                    .WithProperties(
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

            context.SetResource(Guid.NewGuid(),
                new GraphNode {
                    Name = "Root"
                }
                .WithChild(
                    new GraphNode {
                        Name = "Sun",
                        Id = _sunId,
                        Position = new Vector3(0, 1, 5),
                        Rotation = Quaternion.CreateFromYawPitchRoll(-45, -45, 0)
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

            var sceneNode = EmbededAssets.Load<Model>(
                "Nagule.Examples.Embeded.Models.library_earthquake.glb").RootNode;
            context.SetResource(Guid.NewGuid(), sceneNode.MakeOccluder());

            context.SetResource(Guid.NewGuid(),
                EmbededAssets.Load<Model>("Nagule.Examples.Embeded.Models.vanilla_nekopara_fanart.glb").RootNode);
            
            var planeNode = EmbededAssets.Load<Model>(
                "Nagule.Examples.Embeded.Models.plane.glb").RootNode;

            context.SetResource(Guid.NewGuid(), planeNode with {
                Position = new Vector3(0, 0.2f, 0),
                Scale = new Vector3(1.5f),
                MeshRenderable = planeNode.MeshRenderable!.ConvertMeshes(
                    mesh => mesh with {
                        Material = mesh.Material
                            .WithProperties(
                                new(MaterialKeys.Diffuse, new Vector4(1f)),
                                new(MaterialKeys.DiffuseTex, LoadTexture("Nagule.Examples.Embeded.Textures.Substance_Graph_BaseColor.jpg", TextureType.Color)),
                                new(MaterialKeys.Specular, new Vector4(0.2f)),
                                new(MaterialKeys.RoughnessTex, LoadTexture("Nagule.Examples.Embeded.Textures.Substance_Graph_Roughness.jpg", TextureType.Roughness)),
                                new(MaterialKeys.Shininess, 32f),
                                new(MaterialKeys.NormalTex, LoadTexture("Nagule.Examples.Embeded.Textures.Substance_Graph_Normal.jpg", TextureType.Normal)),
                                new(MaterialKeys.HeightTex, LoadTexture("Nagule.Examples.Embeded.Textures.Substance_Graph_Height.jpg", TextureType.Height)),
                                new(MaterialKeys.AmbientOcclusionTex, LoadTexture("Nagule.Examples.Embeded.Textures.Substance_Graph_Roughness.jpg", TextureType.AmbientOcclusion)),
                                new(MaterialKeys.ParallaxScale, 0.05f))
                    })
            });

            var heightTex = LoadTexture(
                "Nagule.Examples.Embeded.Textures.iceland_heightmap.png", TextureType.Color);

            context.SetResource(Guid.NewGuid(), planeNode with {
                Position = new Vector3(-3f, 0.5f, 0),
                Scale = new Vector3(1.5f),
                MeshRenderable = planeNode.MeshRenderable!.ConvertMeshes(
                    mesh => mesh with {
                        Material = (mesh.Material with { RenderMode = RenderMode.Transparent })
                            .WithProperties(
                                new(MaterialKeys.Diffuse, new Vector4(1f, 1f, 1f, 1.5f)),
                                new(MaterialKeys.DiffuseTex, heightTex),
                                new(MaterialKeys.OpacityTex, heightTex),
                                new(MaterialKeys.Threshold, 0.001f),
                                new(MaterialKeys.HeightTex, heightTex),
                                new(MaterialKeys.ParallaxScale, 0.1f),
                                new(MaterialKeys.EnableParallaxEdgeClip),
                                new(MaterialKeys.EnableParallaxShadow))
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

        private Texture LoadTexture(string path, TextureType type)
            => new Texture {
                Image = EmbededAssets.Load<Image>(path),
                Type = type
            };

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
        }

        public struct SceneState : ISingletonComponent
        {
            public Vector3 SunRotation = Vector3.Zero;

            public float Rate = 10;
            public float Sensitivity = 0.005f;
            public float X = 0;
            public float Y = 0;
            public Vector3 DeltaPos = Vector3.Zero;
            public bool Moving = false;
            public bool ControlActive = true;

            public SceneState() {}
        }

        float Lerp(float firstFloat, float secondFloat, float by)
            => firstFloat * (1 - by) + secondFloat * by;
        
        public void OnUpdate(IContext context)
        {
            ImGui.ShowDemoWindow();
            
            ref var state = ref context.AcquireAny<SceneState>(out bool exists);
            if (!exists) {
                state.SunRotation = context.Acquire<Transform>(_sunId).Angles;
            }

            ref var sunRot = ref state.SunRotation;

            ImGui.Begin("Sun control");
            if (ImGui.SliderFloat("X", ref sunRot.X, 0, 360)
                    | ImGui.SliderFloat("Y", ref sunRot.Y, 0, 360)
                    | ImGui.SliderFloat("Z", ref sunRot.Z, 0, 360)) {
                ref var sunTrans = ref context.Acquire<Transform>(_sunId);
                sunTrans.Angles = sunRot;
            }
            ImGui.End();

            ref CameraRenderDebug GetDebug(IContext context)
                => ref context.Acquire<CameraRenderDebug>(_cameraId);
            
            ref readonly var window = ref context.InspectAny<Window>();
            ref readonly var mouse = ref context.InspectAny<Mouse>();
            ref readonly var keyboard = ref context.InspectAny<Keyboard>();

            var keys = keyboard.Keys;

            float deltaTime = context.DeltaTime;
            float scaledRate = deltaTime * state.Rate;

            if (ImGui.IsKeyDown(ImGuiKey.Escape)) {
                context.Unload();
                return;
            }

            if (keys[Key.Space].Down && _lightsId != Guid.Empty) {
                context.Destroy(_lightsId);
                context.Destroy(_toriId);
                _lightsId = Guid.Empty;
                _toriId = Guid.Empty;
            }

            if (keys[Key.Q].Pressed) {
                context.Acquire<Transform>(_toriId).LocalScale += deltaTime * Vector3.One;
            }
            if (keys[Key.E].Pressed) {
                context.Acquire<Transform>(_toriId).LocalScale -= deltaTime * Vector3.One;
            }

            foreach (var rotatorId in _rotators.Query(context)) {
                ref var transform = ref context.Acquire<Transform>(rotatorId);
                transform.Position += transform.Forward * deltaTime * context.Inspect<Rotator>(rotatorId).Speed;
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, context.Time);
            }

            if (keys[Key.F1].Down) {
                context.RemoveAny<CameraRenderDebug>();
            }
            if (keys[Key.F2].Down) {
                GetDebug(context).DisplayMode = DisplayMode.TransparencyAccum;
            }
            if (keys[Key.F3].Down) {
                GetDebug(context).DisplayMode = DisplayMode.TransparencyAlpha;
            }
            if (keys[Key.F4].Down) {
                GetDebug(context).DisplayMode = DisplayMode.Depth;
            }
            if (keys[Key.F5].Down) {
                GetDebug(context).DisplayMode = DisplayMode.Clusters;
            }

            if (keys[Key.C].Down) {
                state.ControlActive = !state.ControlActive;
            }

            if (!state.ControlActive) {
                return;
            }

            ref float x = ref state.X;
            ref float y = ref state.Y;
            x = Lerp(x, (mouse.X - window.Width / 2) * state.Sensitivity, scaledRate);
            y = Lerp(y, (mouse.Y - window.Height / 2) * state.Sensitivity, scaledRate);

            ref var cameraTrans = ref context.Acquire<Transform>(_cameraId);
            cameraTrans.Rotation = Quaternion.CreateFromYawPitchRoll(-x, -y, 0);

            var deltaPos = Vector3.Zero;
            bool modified = false;

            if (keys[Key.W].Pressed) {
                deltaPos += cameraTrans.Forward;
                modified = true;
                state.Moving = true;
            }
            if (keys[Key.S].Pressed) {
                deltaPos -= cameraTrans.Forward;
                modified = true;
                state.Moving = true;
            }
            if (keys[Key.A].Pressed) {
                deltaPos -= cameraTrans.Right;
                modified = true;
                state.Moving = true;
            }
            if (keys[Key.D].Pressed) {
                deltaPos += cameraTrans.Right;
                modified = true;
                state.Moving = true;
            }
            if (state.Moving) {
                state.DeltaPos = Vector3.Lerp(state.DeltaPos, deltaPos, scaledRate);
                if (!modified && state.DeltaPos.Length() < 0.001f) {
                    state.Moving = false;
                    state.DeltaPos = Vector3.Zero;
                }
                else {
                    cameraTrans.Position += state.DeltaPos * deltaTime * 5;
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