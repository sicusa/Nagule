namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using Silk.NET.Assimp;

using AssimpString = Silk.NET.Assimp.AssimpString;
using AssimpMetadataType = Silk.NET.Assimp.MetadataType;
using AssimpLightType = Silk.NET.Assimp.LightSourceType;
using AssimpTextureType = Silk.NET.Assimp.TextureType;
using AssimpTextureMapMode = Silk.NET.Assimp.TextureMapMode;

using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpCamera = Silk.NET.Assimp.Camera;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpLight = Silk.NET.Assimp.Light;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpMaterialProperty = Silk.NET.Assimp.MaterialProperty;
using AssimpTexture = Silk.NET.Assimp.Texture;
using AssmipFace = Silk.NET.Assimp.Face;

public static class ModelUtils
{
    private record struct CameraEntry(Camera3DAsset Asset, IntPtr Pointer);
    private record struct LightEntry(Light3DAsset Asset, IntPtr Pointer);

    private unsafe class AssimpLoaderState
    {
        public AssimpScene* Scene;
        public Dictionary<string, CameraEntry> LoadedCameras = [];
        public Dictionary<string, LightEntry> LoadedLights = [];
        public Dictionary<IntPtr, Mesh3DAsset> LoadedMeshes = [];
        public Dictionary<IntPtr, MaterialAsset> LoadedMaterials = [];
        public Dictionary<string, Texture2DAsset> LoadedTextures = [];
        public Dictionary<string, IntPtr> EmbeddedTextures = [];
    }

    private static readonly Assimp _assimp = Assimp.GetApi();

    public unsafe static Model3DAsset Load(Stream stream, string? name = null)
    {
        var formatHint = name != null ? name[name.LastIndexOf('.')..] : "";
        AssimpScene* scene;

        using (var ms = new MemoryStream()) {
            stream.CopyTo(ms);
            var span = ms.ToArray().AsSpan();
            unchecked {
                var config = PostProcessPreset.TargetRealTimeMaximumQuality
                    | PostProcessSteps.CalculateTangentSpace
                    | PostProcessSteps.ImproveCacheLocality
                    | (PostProcessSteps)0x80000000; // aiProcess_GenBoundingBoxes 
                scene = _assimp.ImportFileFromMemory(
                    span, (uint)span.Length, (uint)config, formatHint);
            }
        }

        if (scene == null) {
            throw new FileLoadException("Model not supported", name);
        }

        var state = new AssimpLoaderState {
            Scene = scene
        };
        LoadLights(state);
        LoadEmbeddedTextures(state);

        return new Model3DAsset(LoadNode(state, scene->MRootNode)) {
            Name = name ?? ""
        };
    }

    private unsafe static void LoadCameras(AssimpLoaderState state)
    {
        var scene = state.Scene;
        var cameraEntries = state.LoadedCameras;

        uint cameraCount = scene->MNumCameras;
        var cameras = scene->MCameras;

        for (int i = 0; i != cameraCount; ++i) {
            var camera = cameras[i];
            if (LoadCamera(state, camera) is Camera3DAsset cameraAsset) {
                cameraEntries[camera->MName] = new(cameraAsset, (IntPtr)camera);
            }
        }
    }

    private unsafe static Camera3DAsset? LoadCamera(AssimpLoaderState state, AssimpCamera* camera)
        => new() {
            Name = camera->MName,
            ProjectionMode = camera->MOrthographicWidth == 0
                ? ProjectionMode.Perspective : ProjectionMode.Orthographic,
            AspectRatio = camera->MAspect,
            FieldOfView = camera->MHorizontalFOV,
            OrthographicWidth = camera->MOrthographicWidth,
            NearPlaneDistance = camera->MClipPlaneNear,
            FarPlaneDistance = camera->MClipPlaneFar
        };

    private unsafe static void LoadLights(AssimpLoaderState state)
    {
        var scene = state.Scene;
        var lightEntries = state.LoadedLights;

        uint lightCount = scene->MNumLights;
        var lights = scene->MLights;

        for (uint i = 0; i != lightCount; ++i) {
            var light = lights[i];
            if (LoadLight(state, light) is Light3DAsset lightAsset) {
                lightEntries[light->MName] = new(lightAsset, (IntPtr)light);
            }
        }
    }

    private unsafe static Light3DAsset? LoadLight(AssimpLoaderState state, AssimpLight* light)
        => light->MType switch {
            AssimpLightType.Directional => new() {
                Name = light->MName,
                Type = LightType.Directional,
                Color = new Vector4(light->MColorDiffuse, 1)
            },
            AssimpLightType.Ambient => new() {
                Name = light->MName,
                Type = LightType.Ambient,
                Color = new Vector4(light->MColorDiffuse, 1)
            },
            AssimpLightType.Point => new() {
                Name = light->MName,
                Type = LightType.Point,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
            },
            AssimpLightType.Spot => new() {
                Name = light->MName,
                Type = LightType.Spot,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
                InnerConeAngle = light->MAngleInnerCone.ToDegree(),
                OuterConeAngle = light->MAngleOuterCone.ToDegree()
            },
            AssimpLightType.Area => new() {
                Name = light->MName,
                Type = LightType.Spot,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
                InnerConeAngle = Math.Min(light->MSize.X, light->MSize.Y),
                OuterConeAngle = Math.Max(light->MSize.X, light->MSize.Y)
            },
            _ => null
        };
    
    private static unsafe float CalculateLightRange(AssimpLight* light)
    {
        float c = light->MAttenuationConstant;
        float l = light->MAttenuationLinear;
        float q = light->MAttenuationQuadratic;
        return (-l + MathF.Sqrt(l * l - 4 * q * (c - 255))) / (2 * q);
    }

    private unsafe static void LoadEmbeddedTextures(AssimpLoaderState state)
    {
        var scene = state.Scene;
        var textures = state.EmbeddedTextures;

        for (int i = 0; i != scene->MNumTextures; ++i) {
            var tex = scene->MTextures[i];
            var key = tex->MFilename.Length != 0 ? tex->MFilename.ToString() : "*" + i;
            textures[key] = (IntPtr)tex;
        }
    }

    private unsafe static Node3DAsset LoadNode(AssimpLoaderState state, AssimpNode* node)
    {
        var scene = state.Scene;
        var childrenBuilder = ImmutableList.CreateBuilder<Node3DAsset>();
        var featureBuilder = ImmutableList.CreateBuilder<FeatureAssetBase>();

        var transform = Matrix4x4.Transpose(node->MTransformation);
        Matrix4x4.Decompose(transform,
            out var scale, out var rotation, out var position);
        
        if (state.LoadedCameras.TryGetValue(node->MName, out var cameraEntry)) {
            var camera = (AssimpCamera*)cameraEntry.Pointer;
            childrenBuilder.Add(new Node3DAsset {
                Name = camera->MName,
                Position = camera->MPosition,
                Rotation = MathUtils.LookRotation(camera->MLookAt, camera->MUp),
                Features = [cameraEntry.Asset]
            });
        }
        
        if (state.LoadedLights.TryGetValue(node->MName, out var lightEntry)) {
            var light = (AssimpLight*)lightEntry.Pointer;
            childrenBuilder.Add(new Node3DAsset {
                Name = light->MName,
                Position = light->MPosition,
                Rotation = MathUtils.LookRotation(light->MDirection, light->MUp),
                Features = [lightEntry.Asset]
            });
        }

        uint meshCount = node->MNumMeshes;
        if (meshCount != 0) {
            var meshes = node->MMeshes;
            for (uint i = 0; i != meshCount; ++i) {
                featureBuilder.Add(LoadMesh(state, scene->MMeshes[meshes[i]]));
            }
        }

        uint childrenNodeCount = node->MNumChildren;
        if (childrenNodeCount != 0) {
            var children = node->MChildren;
            for (uint i = 0; i != childrenNodeCount; ++i) {
                childrenBuilder.Add(LoadNode(state, children[i]));
            }
        }
        
        var metadata = node->MMetaData;

        return new Node3DAsset {
            Name = node->MName,
            Position = position,
            Rotation = rotation,
            Scale = scale,
            Metadata = metadata != null
                ? FromMetadata(metadata)
                : ImmutableDictionary<string, Dyn>.Empty,
            Features = featureBuilder.ToImmutable(),
            Children = childrenBuilder.ToImmutable()
        };
    }

    private unsafe static Mesh3DAsset LoadMesh(AssimpLoaderState state, AssimpMesh* mesh)
    {
        if (state.LoadedMeshes.TryGetValue((IntPtr)mesh, out var result)) {
            return result;
        }

        var scene = state.Scene;

        var primitiveType = mesh->MPrimitiveTypes switch {
            0x1 => PrimitiveType.Point,
            0x2 => PrimitiveType.Line,
            0x4 => PrimitiveType.Triangle,
            0x8 => PrimitiveType.Polygon,
            _ => PrimitiveType.Triangle,
        };
        int vertCount = (int)mesh->MNumVertices;

        ImmutableArray<Vector3> LoadVectors(Vector3* vs)
            => vs != null
                ? new Span<Vector3>(vs, vertCount).ToArray().ToImmutableArray()
                : ImmutableArray<Vector3>.Empty;
        
        var vertices = LoadVectors(mesh->MVertices);
        var normals = LoadVectors(mesh->MNormals);
        var texCoords = LoadVectors(mesh->MTextureCoords.Element0);
        var tangents = LoadVectors(mesh->MTangents);

        var indicesBuilder = ImmutableArray.CreateBuilder<uint>();
        if (mesh->MFaces != null) {
            var faces = new Span<AssmipFace>(mesh->MFaces, (int)mesh->MNumFaces);
            foreach (ref var face in faces) {
                var mIndices = face.MIndices;
                for (int i = 0; i != face.MNumIndices; ++i) {
                    indicesBuilder.Add(mIndices[i]);
                }
            }
        }

        var aabb = mesh->MAABB;
        var boundingBox = new Rectangle(
            new(aabb.Min.X, aabb.Min.Y, aabb.Min.Z),
            new(aabb.Max.X, aabb.Max.Y, aabb.Max.Z));

        result = new Mesh3DAsset {
            Name = mesh->MName,
            Data = new Mesh3DData {
                PrimitiveType = primitiveType,
                Vertices = vertices,
                Normals = normals,
                TexCoords = texCoords,
                Tangents = tangents,
                Indices = indicesBuilder.ToImmutable(),
                BoundingBox = boundingBox,
            },
            Material = LoadMaterial(state, scene->MMaterials[mesh->MMaterialIndex])
        };

        state.LoadedMeshes[(IntPtr)mesh] = result;
        return result;
    }

    private class MaterialProperties
    {
        private Dictionary<string, IntPtr> _props = [];

        public unsafe MaterialProperties(AssimpMaterial* mat)
        {
            for (int i = 0; i != mat->MNumProperties; ++i) {
                var prop = mat->MProperties[i];
                _props[prop->MKey] = (IntPtr)prop;
            }
        }

        public bool Contains(string key)
            => _props.ContainsKey(key);

        public unsafe T? Get<T>(string key, T? defaultValue = default(T))
        {
            if (!_props!.TryGetValue(key, out var propRaw)) {
                return defaultValue;
            }
            var prop = (AssimpMaterialProperty*)propRaw;
            return Unsafe.AsRef<T>(prop->MData);
        }

        public unsafe Vector4 GetColor(string key)
        {
            if (!_props!.TryGetValue(key, out var propRaw)) {
                return Vector4.Zero;
            }
            var prop = (AssimpMaterialProperty*)propRaw;
            if (prop->MDataLength >= Unsafe.SizeOf<Vector4>()) {
                return Unsafe.AsRef<Vector4>(prop->MData);
            }
            else if (prop->MDataLength >= Unsafe.SizeOf<Vector3>()) {
                return new Vector4(Unsafe.AsRef<Vector3>(prop->MData), 1);
            }
            return Vector4.Zero;
        }

        public unsafe string? GetString(string key)
        {
            if (!_props!.TryGetValue(key, out var propRaw)) {
                return null;
            }
            var prop = (AssimpMaterialProperty*)propRaw;
            var raw = (AssimpString*)prop->MData;
            return raw->Length != 0 ? raw->ToString() : "";
        }
    }

    private unsafe static MaterialAsset LoadMaterial(AssimpLoaderState state, AssimpMaterial* mat)
    {
        if (state.LoadedMaterials.TryGetValue((IntPtr)mat, out var materialRes)) {
            return materialRes;
        }

        var renderMode = RenderMode.Opaque;
        var props = new MaterialProperties(mat);
        var parsBuilder = ImmutableDictionary.CreateBuilder<string, Dyn>();
        bool isTwoSided = props.Get<int>(Assimp.MatkeyTwosided) == 1;

        // caluclate diffuse color

        var diffuse = props.GetColor(Assimp.MatkeyColorDiffuse);
        var opacity = props.Get<float>(Assimp.MatkeyOpacity, 1);
        var transparentFactor = props.Get<float>(Assimp.MatkeyTransparencyfactor, 0);

        if (opacity != 1) {
            renderMode = RenderMode.Transparent;
            diffuse.W *= opacity;
        }
        if (transparentFactor != 0) {
            renderMode = RenderMode.Transparent;
            diffuse.W *= 1 - transparentFactor;
        }
        if (props.Contains(Assimp.MatkeyColorTransparent)) {
            renderMode = RenderMode.Transparent;
            diffuse *= Vector4.One - props.GetColor(Assimp.MatkeyColorTransparent);
        }

        // calculate specular color
        
        var specular = props.GetColor(Assimp.MatkeyColorSpecular)
            * props.Get<float>(Assimp.MatkeyShininessStrength, 1);
        
        // load numeric parameters

        parsBuilder[MaterialKeys.Diffuse.Name] = Dyn.From(diffuse);

        if (!IsBlackColor(specular)) {
            parsBuilder[MaterialKeys.Specular.Name] = Dyn.From(specular);
        }
        var ambient = props.GetColor(Assimp.MatkeyColorAmbient);
        if (!IsBlackColor(ambient)) {
            parsBuilder[MaterialKeys.Ambient.Name] = Dyn.From(ambient);
        }
        var emissive = props.GetColor(Assimp.MatkeyColorEmissive);
        if (!IsBlackColor(emissive)) {
            parsBuilder[MaterialKeys.Emission.Name] = Dyn.From(emissive);
        }

        float shininess = props.Get<float>(Assimp.MatkeyShininess, 0);
        if (shininess != 0) {
            parsBuilder[MaterialKeys.Shininess.Name] = Dyn.From(shininess);
        }
        
        // load textures

        void TryLoadTexture(AssimpTextureType type)
        {
            var tex = LoadTexture(state, mat, type);
            if (tex != null) {
                parsBuilder[Enum.GetName(FromTextureType(type)) + "Tex"] = new TextureDyn(tex);
            }
        }

        var tex = LoadTexture(state, mat, AssimpTextureType.Diffuse)!;
        if (tex != null) {
            if (tex.Image!.PixelFormat == PixelFormat.RedGreenBlueAlpha) {
                if (renderMode != RenderMode.Transparent) {
                    renderMode = RenderMode.Cutoff;
                    isTwoSided = true;
                }
            }
            parsBuilder[MaterialKeys.DiffuseTex.Name] = new TextureDyn(tex);
        }

        TryLoadTexture(AssimpTextureType.Specular);
        TryLoadTexture(AssimpTextureType.Ambient);
        TryLoadTexture(AssimpTextureType.Emissive);
        TryLoadTexture(AssimpTextureType.Height);
        TryLoadTexture(AssimpTextureType.Normals);
        TryLoadTexture(AssimpTextureType.Opacity);
        TryLoadTexture(AssimpTextureType.Displacement);
        TryLoadTexture(AssimpTextureType.Lightmap);
        TryLoadTexture(AssimpTextureType.Reflection);
        TryLoadTexture(AssimpTextureType.AmbientOcclusion);

        // load shaders

        var shaderProgram = GLSLProgramAsset.Standard;
        
        void TryLoadShader(ShaderType type, string key)
        {
            var shader = props!.GetString(key);
            if (!string.IsNullOrEmpty(shader)) {
                shaderProgram = shaderProgram.WithShader(type, shader);
            }
        }

        TryLoadShader(ShaderType.Vertex, Assimp.MatkeyShaderVertex);
        TryLoadShader(ShaderType.Fragment, Assimp.MatkeyShaderFragment);
        TryLoadShader(ShaderType.Geometry, Assimp.MatkeyShaderGeo);
        TryLoadShader(ShaderType.Compute, Assimp.MatkeyShaderCompute);

        // finish

        materialRes = new MaterialAsset {
            Name = props.GetString(Assimp.MatkeyName) ?? "",
            ShaderProgram = shaderProgram,
            RenderMode = renderMode,
            IsTwoSided = isTwoSided,
            Properties = parsBuilder.ToImmutable()
        };

        state.LoadedMaterials[(IntPtr)mat] = materialRes;
        return materialRes;
    }

    private unsafe static Texture2DAsset? LoadTexture(
        AssimpLoaderState state, AssimpMaterial* mat, AssimpTextureType type)
    {
        AssimpString pathRaw;
        AssimpTextureMapMode mapModeRaw;
        _assimp.GetMaterialTexture(mat, type, 0, &pathRaw, null, null, null, null, &mapModeRaw, null);

        var path = pathRaw.ToString();
        if (string.IsNullOrEmpty(path)) {
            return null;
        }

        if (state.LoadedTextures.TryGetValue(path, out var textureResource)) {
            return textureResource;
        }

        var mapMode = FromTextureMapMode(mapModeRaw);
        try {
            textureResource = new Texture2DAsset {
                Name = path,
                Image = LoadImage(state, path),
                Type = FromTextureType(type),
                WrapU = mapMode,
                WrapV = mapMode
            };
            state.LoadedTextures[path] = textureResource;
            return textureResource;
        }
        catch (Exception e) {
            Console.WriteLine("Failed to load texture: " + e);
            return Texture2DAsset.Hint;
        }
    }

    private unsafe static ImageAsset LoadImage(AssimpLoaderState state, string path)
    {
        if (!state.EmbeddedTextures.TryGetValue(path, out var ptr)) {
            return ImageUtils.Load(System.IO.File.OpenRead(path));
        }

        var embeddedTexture = (AssimpTexture*)ptr;
        int height = (int)embeddedTexture->MHeight;
        int width = (int)embeddedTexture->MWidth;

        bool hasAlpha = false;
        byte[] bytes;

        if (height == 0) {
            return ImageUtils.Load(
                new Span<byte>(embeddedTexture->PcData, (int)embeddedTexture->MWidth).ToArray());
        }

        var texels = new Span<Texel>(embeddedTexture->PcData, height * width);

        foreach (ref var texel in texels) {
            if (texel.A != 255) {
                hasAlpha = true;
                break;
            }
        }

        if (hasAlpha) {
            bytes = new byte[texels.Length * 4];
            for (int i = 0; i < texels.Length; ++i) {
                ref var texel = ref texels[i];
                bytes[i * 4] = texel.R;
                bytes[i * 4 + 1] = texel.G;
                bytes[i * 4 + 2] = texel.B;
                bytes[i * 4 + 3] = texel.A;
            }
        }
        else {
            bytes = new byte[texels.Length * 3];
            for (int i = 0; i < texels.Length; ++i) {
                ref var texel = ref texels[i];
                bytes[i * 3] = texel.R;
                bytes[i * 3 + 1] = texel.G;
                bytes[i * 3 + 2] = texel.B;
            }
        }

        return new() {
            Width = width,
            Height = height,
            Data = bytes.ToImmutableArray(),
            PixelFormat = hasAlpha ? PixelFormat.RedGreenBlueAlpha : PixelFormat.RedGreenBlue
        };
    }

    private static unsafe ImmutableDictionary<string, Dyn> FromMetadata(Metadata* metadata)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, Dyn>();
        var keys = metadata->MKeys;
        var values = metadata->MValues;

        for (int i = 0; i != metadata->MNumProperties; ++i) {
            var value = values[i];
            Dyn? result = value.MType switch {
                AssimpMetadataType.Bool => new Dyn.Bool(*(bool*)value.MData),
                AssimpMetadataType.Int32 => new Dyn.Int(*(int*)value.MData),
                AssimpMetadataType.Uint64 => new Dyn.ULong(*(ulong*)value.MData),
                AssimpMetadataType.Float => new Dyn.Float(*(float*)value.MData),
                AssimpMetadataType.Double => new Dyn.Double(*(double*)value.MData),
                AssimpMetadataType.Aivector3D => new Dyn.Vector3(*(Vector3*)value.MData),
                AssimpMetadataType.Aistring => new Dyn.String(((AssimpString*)value.MData)->ToString()),
                AssimpMetadataType.Aimetadata => new Dyn.StringMap(FromMetadata((Metadata*)value.MData)),
                _ => null
            };
            if (result == null) {
                continue;
            }

            var key = keys[i];
            if (builder.TryGetValue(key, out var prev)) {
                builder[key] = prev is Dyn.Array arr
                    ? new Dyn.Array(arr.Elements.Add(result))
                    : new Dyn.Array([prev, result]);
            }
            else {
                builder[key] = result;
            }
        }
        return builder.ToImmutable();
    }

    private static bool IsBlackColor(Vector4 v)
        => v.X == 0 && v.Y == 0 && v.Z == 0;
    
    private static TextureType FromTextureType(AssimpTextureType type)
        => type switch {
            AssimpTextureType.Diffuse => TextureType.Color,
            AssimpTextureType.Specular => TextureType.Specular,
            AssimpTextureType.Ambient => TextureType.Ambient,
            AssimpTextureType.Emissive => TextureType.Emissive,
            AssimpTextureType.Displacement => TextureType.Displacement,
            AssimpTextureType.Height => TextureType.Height,
            AssimpTextureType.Lightmap => TextureType.Lightmap,
            AssimpTextureType.Normals => TextureType.Normal,
            AssimpTextureType.Opacity => TextureType.Opacity,
            AssimpTextureType.Reflection => TextureType.Reflection,
            AssimpTextureType.AmbientOcclusion => TextureType.AmbientOcclusion,
            _ => TextureType.Unknown
        };
    
    private static TextureWrapMode FromTextureMapMode(AssimpTextureMapMode mode)
        => mode switch {
            AssimpTextureMapMode.Clamp => TextureWrapMode.ClampToEdge,
            AssimpTextureMapMode.Decal => TextureWrapMode.ClampToBorder,
            AssimpTextureMapMode.Mirror => TextureWrapMode.MirroredRepeat,
            AssimpTextureMapMode.Wrap => TextureWrapMode.Repeat,
            _ => TextureWrapMode.Repeat
        };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle CalculateBoundingBox(ReadOnlySpan<Vector3> vertices)
    {
        var min = new Vector3();
        var max = new Vector3();

        foreach (ref readonly var vertex in vertices) {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        return new Rectangle(min, max);
    }
}