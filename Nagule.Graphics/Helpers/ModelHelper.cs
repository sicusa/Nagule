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

public static class ModelHelper
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
                    : ImmutableDictionary<string, object>.Empty,

            Lights = lights,

            Meshes = node->MNumMeshes != 0
                ? Enumerable.Range(0, (int)node->MNumMeshes)
                    .Select(i => LoadMesh(state, scene->MMeshes[meshes[i]])).ToImmutableList()
                : ImmutableList<Mesh>.Empty,

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
        var bitangents = LoadVectors(mesh->MBitangents);

        var indicesBuilder = ImmutableArray.CreateBuilder<int>();
        if (mesh->MFaces != null) {
            var faces = new Span<AssmipFace>(mesh->MFaces, (int)mesh->MNumFaces);
            foreach (ref var face in faces) {
                var mIndices = face.MIndices;
                for (int i = 0; i != face.MNumIndices; ++i) {
                    indicesBuilder.Add((int)mIndices[i]);
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
            Bitangents = bitangents,
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
                AttenuationConstant = light->MAttenuationConstant,
                AttenuationLinear = light->MAttenuationLinear,
                AttenuationQuadratic = light->MAttenuationQuadratic
            },
            AssimpLightType.Spot => new Light {
                Name = light->MName,
                Type = LightType.Spot,
                Color = new Vector4(light->MColorDiffuse, 1),
                AttenuationConstant = light->MAttenuationConstant,
                AttenuationLinear = light->MAttenuationLinear,
                AttenuationQuadratic = light->MAttenuationQuadratic,
                InnerConeAngle = light->MAngleInnerCone.ToDegree(),
                OuterConeAngle = light->MAngleOuterCone.ToDegree()
            },
            AssimpLightType.Area => new Light {
                Name = light->MName,
                Type = LightType.Area,
                Color = new Vector4(light->MColorDiffuse, 1),
                AttenuationConstant = light->MAttenuationConstant,
                AttenuationLinear = light->MAttenuationLinear,
                AttenuationQuadratic = light->MAttenuationQuadratic,
                AreaSize = light->MSize
            },
            _ => null
        };

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

        bool isTwoSided = props.Get<int>(Assimp.MatkeyTwosided) == 1;
        var customParameters = ImmutableDictionary<string, object>.Empty;

        // caluclate diffuse color

        var diffuseColor = props.GetColor(Assimp.MatkeyColorDiffuse);
        var opacity = props.Get<float>(Assimp.MatkeyOpacity, 1);
        var transparentFactor = props.Get<float>(Assimp.MatkeyTransparencyfactor, 0);

        if (opacity != 1) {
            renderMode = RenderMode.Transparent;
            diffuseColor.W *= opacity;
        }
        if (transparentFactor != 0) {
            renderMode = RenderMode.Transparent;
            diffuseColor.W *= 1 - transparentFactor;
        }
        if (props.Contains(Assimp.MatkeyColorTransparent)) {
            renderMode = RenderMode.Transparent;
            diffuseColor *= Vector4.One - props.GetColor(Assimp.MatkeyColorTransparent);
        }

        // calculate specular color
        
        var specularColor = props.GetColor(Assimp.MatkeyColorSpecular)
            * props.Get<float>(Assimp.MatkeyShininessStrength, 1);
        
        // add textures

        var textures = ImmutableDictionary.CreateBuilder<TextureType, Texture>();

        void TryLoadTexture(AssimpTextureType type)
        {
            var tex = LoadTexture(state, mat, type);
            if (tex != null) {
                textures[FromTextureType(type)] = tex;
            }
        }

        var tex = LoadTexture(state, mat, AssimpTextureType.Diffuse)!;
        if (tex != null) {
            textures[TextureType.Diffuse] = tex;

            if (tex.Image!.PixelFormat == PixelFormat.RedGreenBlueAlpha) {
                if (renderMode != RenderMode.Transparent) {
                    renderMode = RenderMode.Cutoff;
                    isTwoSided = true;
                    customParameters = customParameters.Add("Threshold", 0.9f);
                }
            }
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

        ShaderProgram? shaderProgram = null;
        
        void LoadShader(ShaderType type, string key)
        {
            var shader = props!.GetString(key);
            if (!string.IsNullOrEmpty(shader)) {
                shaderProgram ??= new();
                shaderProgram = shaderProgram.WithShaders(
                    KeyValuePair.Create(type, shader));
            }
        }

        LoadShader(ShaderType.Vertex, Assimp.MatkeyShaderVertex);
        LoadShader(ShaderType.Fragment, Assimp.MatkeyShaderFragment);
        LoadShader(ShaderType.Geometry, Assimp.MatkeyShaderGeo);
        LoadShader(ShaderType.Compute, Assimp.MatkeyShaderCompute);

        if (shaderProgram != null) {
            var shaderLang = props.GetString(Assimp.MatkeyGlobalShaderlang);
            if (shaderLang != null) {
                Console.WriteLine($"Shader language '{shaderLang}' not support");
                shaderProgram = null;
            }
        }

        materialRes = new Material {
            Name = props.GetString(Assimp.MatkeyName) ?? "",
            ShaderProgram = shaderProgram,
            RenderMode = renderMode,
            IsTwoSided = isTwoSided,
            Parameters = new MaterialParameters {
                DiffuseColor = diffuseColor,
                SpecularColor = specularColor,
                AmbientColor = props.GetColor(Assimp.MatkeyColorAmbient),
                EmissiveColor = props.GetColor(Assimp.MatkeyColorEmissive),
                Shininess = props.Get<float>(Assimp.MatkeyShininess)
            },
            Textures = textures.ToImmutable(),
            CustomParameters = customParameters
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
            return ImageHelper.LoadFromFile(filePath);
        }

        var embeddedTexture = (AssimpTexture*)ptr;
        int height = (int)embeddedTexture->MHeight;
        int width = (int)embeddedTexture->MWidth;

        bool hasAlpha = false;
        Byte[] bytes;

        if (height == 0) {
            return ImageHelper.Load(
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
            Bytes = bytes.ToImmutableArray(),
            PixelFormat = hasAlpha ? PixelFormat.RedGreenBlueAlpha : PixelFormat.RedGreenBlue
        };
    }

    private static unsafe ImmutableDictionary<string, object> FromMetadata(Metadata* metadata)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object>();
        var keys = metadata->MKeys;
        var values = metadata->MValues;

        for (int i = 0; i != metadata->MNumProperties; ++i) {
            var key = keys[i];
            var value = values[i];
            object result = value.MType switch {
                AssimpMetadataType.Bool => *((bool*)value.MData),
                AssimpMetadataType.Int32 => *((int*)value.MData),
                AssimpMetadataType.Uint64 => *((ulong*)value.MData),
                AssimpMetadataType.Float => *((float*)value.MData),
                AssimpMetadataType.Double => *((Double*)value.MData),
                AssimpMetadataType.Aistring => ((AssimpString*)value.MData)->ToString(),
                AssimpMetadataType.Aivector3D => *((Vector3*)value.MData),
                AssimpMetadataType.Aimetadata => FromMetadata((Metadata*)value.MData),
                _ => "unsupported metadata"
            };

            if (builder.TryGetValue(key, out var prev)) {
                builder[key] = prev is ImmutableList<object> list
                    ? list.Add(value)
                    : ImmutableList.Create(prev, result);
            }
            else {
                builder[key] = result;
            }
        }
        return builder.ToImmutable();
    }
    
    private static TextureType FromTextureType(AssimpTextureType type)
        => type switch {
            AssimpTextureType.Diffuse => TextureType.Diffuse,
            AssimpTextureType.Specular => TextureType.Specular,
            AssimpTextureType.Ambient => TextureType.Ambient,
            AssimpTextureType.Emissive => TextureType.Emissive,
            AssimpTextureType.Displacement => TextureType.Displacement,
            AssimpTextureType.Height => TextureType.Height,
            AssimpTextureType.Lightmap => TextureType.LightMap,
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