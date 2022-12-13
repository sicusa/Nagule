namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

using Assimp.Configs;

public static class ModelHelper
{
    private class AssimpLoaderState
    {
        public Dictionary<Assimp.Mesh, MeshResource> LoadedMeshes = new();
        public Dictionary<Assimp.Material, MaterialResource> LoadedMaterials = new();
        public Dictionary<string, TextureResource> LoadedTextures = new();
        public Dictionary<string, LightResourceBase> LoadedLights = new();
    }

    private static Assimp.AssimpContext? _assimpImporter;

    public static ModelResource Load(Stream stream, string? formatHint = null)
    {
        if (_assimpImporter == null) {
            _assimpImporter = new();
            _assimpImporter.SetConfig(new NormalSmoothingAngleConfig(66));
        }
        var scene = _assimpImporter.ImportFileFromStream(
            stream, Assimp.PostProcessPreset.TargetRealTimeMaximumQuality, formatHint);
        var state = new AssimpLoaderState();
        
        LoadLights(state, scene);
        return new ModelResource(LoadNode(state, scene, scene.RootNode));
    }

    private static void LoadLights(AssimpLoaderState state, Assimp.Scene scene)
    {
        var lights = state.LoadedLights;
        foreach (var light in scene.Lights) {
            if (LoadLight(state, scene, light) is LightResourceBase lightRes) {
                lights[light.Name] = lightRes;
            }
        }
    }

    private static GraphNodeResource LoadNode(AssimpLoaderState state, Assimp.Scene scene, Assimp.Node node)
    {
        var nodeRes = new GraphNodeResource {
            Name = node.Name
        };

        if (node.Metadata.Count != 0) {
            nodeRes.Metadata = new();
            var metadata = nodeRes.Metadata;
            foreach (var (k, v) in node.Metadata) {
                metadata.Add(k,
                    v.DataType == Assimp.MetaDataType.Vector3D
                        ? FromVector(v.DataAs<Assimp.Vector3D>()!.Value) : v);
            }
        }

        var transform = FromMatrix(node.Transform);
        Matrix4x4.Decompose(transform,
            out nodeRes.Scale, out nodeRes.Rotation, out nodeRes.Position);

        if (state.LoadedLights.TryGetValue(node.Name, out var lightRes)) {
            nodeRes.Lights = new[] { lightRes };
        }
        if (node.HasMeshes) {
            nodeRes.Meshes = node.MeshIndices
                .Select(index => LoadMesh(state, scene, scene.Meshes[index])).ToArray();
        }
        if (node.HasChildren) {
            nodeRes.Children = node.Children
                .Select(node => LoadNode(state, scene, node)).ToArray();
        }
        return nodeRes;
    }

    private static MeshResource LoadMesh(AssimpLoaderState state, Assimp.Scene scene, Assimp.Mesh mesh)
    {
        if (state.LoadedMeshes.TryGetValue(mesh, out var meshResource)) {
            return meshResource;
        }

        var vertices = mesh.Vertices.Select(FromVector).ToArray();

        meshResource = new MeshResource {
            Vertices = vertices,
            BoudingBox = CalculateBoundingBox(vertices),
            TexCoords = mesh.TextureCoordinateChannels[0].Select(FromVector).ToArray(),
            Normals = mesh.Normals.Select(FromVector).ToArray(),
            Tangents = mesh.Tangents.Select(FromVector).ToArray(),
            Indeces = mesh.GetIndices(),
            Material = LoadMaterial(state, scene, scene.Materials[mesh.MaterialIndex])
        };

        state.LoadedMeshes[mesh] = meshResource;
        return meshResource;
    }

    private static LightResourceBase? LoadLight(AssimpLoaderState state, Assimp.Scene scene, Assimp.Light light)
        => light.LightType switch {
            Assimp.LightSourceType.Directional => new DirectionalLightResource {
                Color = FromColor(light.ColorDiffuse)
            },
            Assimp.LightSourceType.Ambient => new AmbientLightResource {
                Color = FromColor(light.ColorDiffuse)
            },
            Assimp.LightSourceType.Point => new PointLightResource {
                AttenuationConstant = light.AttenuationConstant,
                AttenuationLinear = light.AttenuationLinear,
                AttenuationQuadratic = light.AttenuationQuadratic
            },
            Assimp.LightSourceType.Spot => new SpotLightResource {
                AttenuationConstant = light.AttenuationConstant,
                AttenuationLinear = light.AttenuationLinear,
                AttenuationQuadratic = light.AttenuationQuadratic,
                InnerConeAngle = light.AngleInnerCone,
                OuterConeAngle = light.AngleOuterCone
            },
            Assimp.LightSourceType.Area => new AreaLightResource {
                AttenuationConstant = light.AttenuationConstant,
                AttenuationLinear = light.AttenuationLinear,
                AttenuationQuadratic = light.AttenuationQuadratic,
                AreaSize = FromVector(light.AreaSize)
            },
            _ => null
        };

    public static Rectangle CalculateBoundingBox(IEnumerable<Vector3> vertices)
    {
        var min = new Vector3();
        var max = new Vector3();

        foreach (var vertex in vertices) {
            min.X = Math.Min(min.X, vertex.X);
            min.Y = Math.Min(min.Y, vertex.Y);
            min.Z = Math.Min(min.Z, vertex.Z);

            max.X = Math.Max(max.X, vertex.X);
            max.Y = Math.Max(max.Y, vertex.Y);
            max.Z = Math.Max(max.Z, vertex.Z);
        }

        return new Rectangle(min, max);
    }

    private static MaterialResource LoadMaterial(AssimpLoaderState state, Assimp.Scene scene, Assimp.Material mat)
    {
        if (state.LoadedMaterials.TryGetValue(mat, out var materialResource)) {
            return materialResource;
        }

        materialResource = new MaterialResource();
        if (mat.HasName) { materialResource.Name = mat.Name; }
        if (mat.HasTwoSided) { materialResource.IsTwoSided = mat.IsTwoSided; }

        ref var pars = ref materialResource.Parameters;
        if (mat.HasColorDiffuse) { pars.DiffuseColor = FromColor(mat.ColorDiffuse); }
        if (mat.HasColorSpecular) { pars.SpecularColor = FromColor(mat.ColorSpecular); }
        if (mat.HasColorAmbient) { pars.AmbientColor = FromColor(mat.ColorAmbient); }
        if (mat.HasColorEmissive) { pars.EmissiveColor = FromColor(mat.ColorEmissive); }
        if (mat.HasShininess && mat.Shininess != 0) { pars.Shininess = mat.Shininess; }
        if (mat.HasShininessStrength) { pars.SpecularColor *= mat.ShininessStrength; }

        if (mat.HasOpacity) {
            if (mat.Opacity != 1) {
                materialResource.RenderMode = RenderMode.Transparent;
                pars.DiffuseColor.W *= mat.Opacity;
            }
            else {
                materialResource.RenderMode = RenderMode.Cutoff;
                materialResource = materialResource.WithParameter("Threshold", 0.9f);
            }
        }
        if (mat.HasTransparencyFactor) {
            materialResource.RenderMode = RenderMode.Transparent;
            pars.DiffuseColor.W *= 1 - mat.TransparencyFactor;
        }
        if (mat.HasColorTransparent) {
            materialResource.RenderMode = RenderMode.Transparent;
            pars.DiffuseColor *= Vector4.One - FromColor(mat.ColorTransparent);
        }

        var textures = ImmutableDictionary.CreateBuilder<TextureType, TextureResource>();

        if (mat.HasTextureDiffuse) { textures[TextureType.Diffuse] = LoadTexture(state, scene, mat.TextureDiffuse); }
        if (mat.HasTextureSpecular) { textures[TextureType.Specular] = LoadTexture(state, scene, mat.TextureSpecular); }
        if (mat.HasTextureAmbient) { textures[TextureType.Ambient] = LoadTexture(state, scene, mat.TextureAmbient); }
        if (mat.HasTextureEmissive) { textures[TextureType.Emissive] = LoadTexture(state, scene, mat.TextureEmissive); }
        if (mat.HasTextureHeight) { textures[TextureType.Height] = LoadTexture(state, scene, mat.TextureHeight); }
        if (mat.HasTextureNormal) { textures[TextureType.Normal] = LoadTexture(state, scene, mat.TextureNormal); }
        if (mat.HasTextureOpacity) { textures[TextureType.Opacity] = LoadTexture(state, scene, mat.TextureOpacity); }
        if (mat.HasTextureDisplacement) { textures[TextureType.Displacement] = LoadTexture(state, scene, mat.TextureDisplacement); }
        if (mat.HasTextureLightMap) { textures[TextureType.LightMap] = LoadTexture(state, scene, mat.TextureLightMap); }
        if (mat.HasTextureReflection) { textures[TextureType.Reflection] = LoadTexture(state, scene, mat.TextureReflection); }

        materialResource.Textures = textures.ToImmutable();

        state.LoadedMaterials[mat] = materialResource;
        return materialResource;
    }

    private static TextureResource LoadTexture(AssimpLoaderState state, Assimp.Scene scene, Assimp.TextureSlot tex)
    {
        if (state.LoadedTextures.TryGetValue(tex.FilePath, out var textureResource)) {
            return textureResource;
        }
        try {
            textureResource = new TextureResource(LoadImage(state, scene, tex.FilePath)) {
                TextureType = FromTextureType(tex.TextureType),
                WrapU = FromTextureWrapMode(tex.WrapModeU),
                WrapV = FromTextureWrapMode(tex.WrapModeV)
            };
            state.LoadedTextures[tex.FilePath] = textureResource;
            return textureResource;
        }
        catch (Exception e) {
            Console.WriteLine("Failed to load texture: " + e);
            return TextureResource.Hint;
        }
    }

    private static ImageResource LoadImage(AssimpLoaderState state, Assimp.Scene scene, string filePath)
    {
        var embeddedTexture = scene.GetEmbeddedTexture(filePath);
        if (embeddedTexture == null) {
            return ImageHelper.LoadFromFile(filePath);
        }
        if (embeddedTexture.HasCompressedData) {
            return ImageHelper.Load(embeddedTexture.CompressedData);
        }
        var data = embeddedTexture.NonCompressedData;
        var bytes = new Byte[data.Length * 4];
        for (int i = 0; i < data.Length; ++i) {
            ref var texel = ref data[i];
            bytes[i * 4] = texel.R;
            bytes[i * 4 + 1] = texel.G;
            bytes[i * 4 + 2] = texel.B;
            bytes[i * 4 + 3] = texel.A;
        }
        return new ImageResource {
            Width = embeddedTexture.Width,
            Height = embeddedTexture.Height,
            Bytes = bytes
        };
    }

    private static Matrix4x4 FromMatrix(Assimp.Matrix4x4 mat)
        => new Matrix4x4(
            mat.A1, mat.B1, mat.C1, mat.D1,
            mat.A2, mat.B2, mat.C2, mat.D2,
            mat.A3, mat.B3, mat.C3, mat.D3,
            mat.A4, mat.B4, mat.C4, mat.D4);

    private static Vector2 FromVector(Assimp.Vector2D v)
        => new Vector2(v.X, v.Y);
    
    private static Vector3 FromVector(Assimp.Vector3D v)
        => new Vector3(v.X, v.Y, v.Z);

    private static Vector4 FromColor(Assimp.Color4D c)
        => new Vector4(c.R, c.G, c.B, c.A);

    private static Vector4 FromColor(Assimp.Color3D c)
        => new Vector4(c.R, c.G, c.B, 1);
    
    private static TextureType FromTextureType(Assimp.TextureType type)
        => type switch {
            Assimp.TextureType.Diffuse => TextureType.Diffuse,
            Assimp.TextureType.Specular => TextureType.Specular,
            Assimp.TextureType.Ambient => TextureType.Ambient,
            Assimp.TextureType.Emissive => TextureType.Emissive,
            Assimp.TextureType.Displacement => TextureType.Displacement,
            Assimp.TextureType.Height => TextureType.Height,
            Assimp.TextureType.Lightmap => TextureType.LightMap,
            Assimp.TextureType.Normals => TextureType.Normal,
            Assimp.TextureType.Opacity => TextureType.Opacity,
            Assimp.TextureType.Reflection => TextureType.Reflection,
            _ => TextureType.Unknown
        };
    
    private static TextureWrapMode FromTextureWrapMode(Assimp.TextureWrapMode mode)
        => mode switch {
            Assimp.TextureWrapMode.Clamp => TextureWrapMode.ClampToEdge,
            Assimp.TextureWrapMode.Decal => TextureWrapMode.ClampToBorder,
            Assimp.TextureWrapMode.Mirror => TextureWrapMode.MirroredRepeat,
            Assimp.TextureWrapMode.Wrap => TextureWrapMode.Repeat,
            _ => TextureWrapMode.Repeat
        };
}