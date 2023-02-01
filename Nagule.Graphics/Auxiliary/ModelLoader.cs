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
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpLight = Silk.NET.Assimp.Light;
using AssimpMaterial = Silk.NET.Assimp.Material;
using AssimpMaterialProperty = Silk.NET.Assimp.MaterialProperty;
using AssimpTexture = Silk.NET.Assimp.Texture;
using AssmipFace = Silk.NET.Assimp.Face;

public static class ModelLoader
{
    private unsafe class AssimpLoaderState
    {
        public AssimpScene* Scene;
        public Dictionary<IntPtr, Mesh> LoadedMeshes = new();
        public Dictionary<IntPtr, Material> LoadedMaterials = new();
        public Dictionary<string, Texture> LoadedTextures = new();
        public Dictionary<string, (Light, IntPtr)> LoadedLights = new();
        public Dictionary<string, IntPtr> EmbeddedTextures = new();
    }

    private static readonly Assimp _assimp = Assimp.GetApi();

    public unsafe static Model Load(Stream stream, string? name = null)
    {
        var formatHint = name != null ? name.Substring(name.LastIndexOf('.')) : "";
        AssimpScene* scene;

        using (MemoryStream ms = new MemoryStream()) {
            stream.CopyTo(ms);
            var span = ms.ToArray().AsSpan();
            unchecked {
                var config = Silk.NET.Assimp.PostProcessPreset.TargetRealTimeMaximumQuality
                    | PostProcessSteps.CalculateTangentSpace
                    | PostProcessSteps.GenerateSmoothNormals
                    | PostProcessSteps.ImproveCacheLocality
                    | (PostProcessSteps)0x80000000; // aiProcess_GenBoundingBoxes 
                scene = _assimp.ImportFileFromMemory(
                    span, (uint)span.Length, (uint)config, formatHint);
            }
        }

        if (scene == null) {
            throw new FileLoadException("Model not supported", name);
        }

        var state = new AssimpLoaderState();
        state.Scene = scene;

        LoadLights(state);
        LoadEmbededTextures(state);

        return new Model(LoadNode(state, scene->MRootNode)) {
            Name = name ?? ""
        };
    }

    private unsafe static void LoadLights(AssimpLoaderState state)
    {
        var scene = state.Scene;
        var lights = state.LoadedLights;

        for (int i = 0; i != scene->MNumLights; ++i) {
            var light = scene->MLights[i];
            if (LoadLight(state, light) is Light lightRes) {
                lights[light->MName] = (lightRes, (IntPtr)light);
            }
        }
    }

    private unsafe static void LoadEmbededTextures(AssimpLoaderState state)
    {
        var scene = state.Scene;
        var textures = state.EmbeddedTextures;

        for (int i = 0; i != scene->MNumTextures; ++i) {
            var tex = scene->MTextures[i];
            var key = tex->MFilename.Length != 0 ? tex->MFilename.ToString() : "*" + i;
            textures[key] = (IntPtr)tex;
        }
    }

    private unsafe static GraphNode LoadNode(AssimpLoaderState state, AssimpNode* node)
    {
        var scene = state.Scene;
        var transform = Matrix4x4.Transpose(node->MTransformation);
        Matrix4x4.Decompose(transform,
            out var scale, out var rotation, out var position);
        
        var metadata = node->MMetaData;
        var meshes = node->MMeshes;

        var lights = ImmutableList<Light>.Empty;
        if (state.LoadedLights.TryGetValue(node->MName, out var res)) {
            var light = res.Item1;
            var assimpLight = (AssimpLight*)res.Item2;

            lights = ImmutableList.Create(res.Item1);

            if (light.Type != LightType.Directional && light.Type != LightType.Ambient) {
                position += assimpLight->MPosition;
            }
            if (light.Type == LightType.Spot) {
                rotation *= MathHelper.LookRotation(assimpLight->MDirection, Vector3.UnitY);
            }
            else if (light.Type == LightType.Area) {
                rotation *= MathHelper.LookRotation(assimpLight->MDirection, assimpLight->MUp);
            }
        }

        return new GraphNode {
            Name = node->MName,
            Position = position,
            Rotation = rotation,
            Scale = scale,

            Metadata =
                metadata != null
                    ? FromMetadata(metadata)
                    : ImmutableDictionary<string, Dyn>.Empty,

            Lights = lights,

            MeshRenderable = node->MNumMeshes != 0
                ? new MeshRenderable {
                    Meshes = Enumerable.Range(0, (int)node->MNumMeshes)
                        .Select(i => LoadMesh(state, scene->MMeshes[meshes[i]]))
                        .ToImmutableHashSet()
                }
                : null,

            Children = node->MNumChildren != 0
                ? Enumerable.Range(0, (int)node->MNumChildren)
                    .Select(i => LoadNode(state, node->MChildren[i])).ToImmutableList()
                : ImmutableList<GraphNode>.Empty
        };
    }

    private unsafe static Mesh LoadMesh(AssimpLoaderState state, AssimpMesh* mesh)
    {
        if (state.LoadedMeshes.TryGetValue((IntPtr)mesh, out var meshResource)) {
            return meshResource;
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
            new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Min.Z),
            new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Max.Z));

        meshResource = new Mesh {
            Name = mesh->MName,
            PrimitiveType = primitiveType,
            Vertices = vertices,
            Normals = normals,
            TexCoords = texCoords,
            Tangents = tangents,
            Indices = indicesBuilder.ToImmutable(),
            BoundingBox = boundingBox,
            Material = LoadMaterial(state, scene->MMaterials[mesh->MMaterialIndex])
        };

        state.LoadedMeshes[(IntPtr)mesh] = meshResource;
        return meshResource;
    }

    private unsafe static Light? LoadLight(AssimpLoaderState state, AssimpLight* light)
        => light->MType switch {
            AssimpLightType.Directional => new Light {
                Name = light->MName,
                Type = LightType.Directional,
                Color = new Vector4(light->MColorDiffuse, 1)
            },
            AssimpLightType.Ambient => new Light {
                Name = light->MName,
                Type = LightType.Ambient,
                Color = new Vector4(light->MColorDiffuse, 1)
            },
            AssimpLightType.Point => new Light {
                Name = light->MName,
                Type = LightType.Point,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
            },
            AssimpLightType.Spot => new Light {
                Name = light->MName,
                Type = LightType.Spot,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
                InnerConeAngle = light->MAngleInnerCone.ToDegree(),
                OuterConeAngle = light->MAngleOuterCone.ToDegree()
            },
            AssimpLightType.Area => new Light {
                Name = light->MName,
                Type = LightType.Area,
                Color = new Vector4(light->MColorDiffuse, 1),
                Range = CalculateLightRange(light),
                AreaSize = light->MSize
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

    private class MaterialProperties
    {
        private Dictionary<string, IntPtr> _props = new();

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

    private unsafe static Material LoadMaterial(AssimpLoaderState state, AssimpMaterial* mat)
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

        parsBuilder[MaterialKeys.Diffuse] = Dyn.From(diffuse);

        if (!IsBlackColor(specular)) {
            parsBuilder[MaterialKeys.Specular] = Dyn.From(specular);
        }
        var ambient = props.GetColor(Assimp.MatkeyColorAmbient);
        if (!IsBlackColor(ambient)) {
            parsBuilder[MaterialKeys.Ambient] = Dyn.From(ambient);
        }
        var emissive = props.GetColor(Assimp.MatkeyColorEmissive);
        if (!IsBlackColor(emissive)) {
            parsBuilder[MaterialKeys.Emission] = Dyn.From(emissive);
        }

        float shininess = props.Get<float>(Assimp.MatkeyShininess, 0);
        if (shininess != 0) {
            parsBuilder[MaterialKeys.Shininess] = Dyn.From(shininess);
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
            parsBuilder[MaterialKeys.DiffuseTex] = new TextureDyn(tex);
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

        GLSLProgram? shaderProgram = null;
        
        void TryLoadShader(ShaderType type, string key)
        {
            var shader = props!.GetString(key);
            if (!string.IsNullOrEmpty(shader)) {
                shaderProgram ??= new();
                shaderProgram = shaderProgram.WithShader(type, shader);
            }
        }

        TryLoadShader(ShaderType.Vertex, Assimp.MatkeyShaderVertex);
        TryLoadShader(ShaderType.Fragment, Assimp.MatkeyShaderFragment);
        TryLoadShader(ShaderType.Geometry, Assimp.MatkeyShaderGeo);
        TryLoadShader(ShaderType.Compute, Assimp.MatkeyShaderCompute);

        if (shaderProgram != null) {
            var shaderLang = props.GetString(Assimp.MatkeyGlobalShaderlang);
            if (shaderLang != null) {
                Console.WriteLine($"Shader language '{shaderLang}' not support");
                shaderProgram = null;
            }
        }

        // finish

        materialRes = new Material {
            Name = props.GetString(Assimp.MatkeyName) ?? "",
            ShaderProgram = shaderProgram,
            RenderMode = renderMode,
            IsTwoSided = isTwoSided,
            Properties = parsBuilder.ToImmutable()
        };

        state.LoadedMaterials[(IntPtr)mat] = materialRes;
        return materialRes;
    }

    private unsafe static Texture? LoadTexture(
        AssimpLoaderState state, AssimpMaterial* mat, AssimpTextureType type)
    {
        AssimpString pathRaw;
        TextureMapMode mapModeRaw;
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
            textureResource = new Texture {
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
            return Texture.Hint;
        }
    }

    private unsafe static Image LoadImage(AssimpLoaderState state, string filePath)
    {
        if (!state.EmbeddedTextures.TryGetValue(filePath, out var ptr)) {
            return ImageLoader.LoadFromFile(filePath);
        }

        var embeddedTexture = (AssimpTexture*)ptr;
        int height = (int)embeddedTexture->MHeight;
        int width = (int)embeddedTexture->MWidth;

        bool hasAlpha = false;
        Byte[] bytes;

        if (height == 0) {
            return ImageLoader.Load(
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
            bytes = new Byte[texels.Length * 4];
            for (int i = 0; i < texels.Length; ++i) {
                ref var texel = ref texels[i];
                bytes[i * 4] = texel.R;
                bytes[i * 4 + 1] = texel.G;
                bytes[i * 4 + 2] = texel.B;
                bytes[i * 4 + 3] = texel.A;
            }
        }
        else {
            bytes = new Byte[texels.Length * 3];
            for (int i = 0; i < texels.Length; ++i) {
                ref var texel = ref texels[i];
                bytes[i * 3] = texel.R;
                bytes[i * 3 + 1] = texel.G;
                bytes[i * 3 + 2] = texel.B;
            }
        }

        return new Image {
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
                AssimpMetadataType.Bool => new Dyn.Bool(*((bool*)value.MData)),
                AssimpMetadataType.Int32 => new Dyn.Int(*((int*)value.MData)),
                AssimpMetadataType.Uint64 => new Dyn.ULong(*((ulong*)value.MData)),
                AssimpMetadataType.Float => new Dyn.Float(*((float*)value.MData)),
                AssimpMetadataType.Double => new Dyn.Double(*((Double*)value.MData)),
                AssimpMetadataType.Aivector3D => new Dyn.Vector3(*((Vector3*)value.MData)),
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
                    : new Dyn.Array(ImmutableArray.Create(prev, result));
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
}