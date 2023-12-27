namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Sia;

public class MaterialManager
    : GraphicsAssetManagerBase<Material, MaterialAsset, Tuple<MaterialState, MaterialReferences>>
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
    
        static void RecreateShaderPrograms(
            World world, in EntityRef entity, ref MaterialReferences references, GLSLProgramAsset colorProgramAsset)
        {
            var programManager = world.GetAddon<GLSLProgramManager>();

            entity.UnreferAsset(references.ColorProgram);
            entity.UnreferAsset(references.DepthProgram);

            references.ColorProgramAsset = colorProgramAsset;
            references.DepthProgramAsset = CreateDepthShaderProgramAsset(colorProgramAsset);

            references.ColorProgram = programManager.Acquire(references.ColorProgramAsset, entity);
            references.DepthProgram = programManager.Acquire(references.DepthProgramAsset, entity);
        }

        Listen((EntityRef entity, ref Material snapshot, in Material.SetRenderMode cmd) => {
            var prevMode = snapshot.RenderMode;
            var mode = cmd.Value;

            var prevMacro = "RenderMode_" + Enum.GetName(prevMode);
            var macro = "RenderMode_" + Enum.GetName(mode);

            ref var matRefs = ref entity.GetState<MaterialReferences>();

            RecreateShaderPrograms(world, entity, ref matRefs, matRefs.ColorProgramAsset with {
                Macros = matRefs.ColorProgramAsset.Macros
                    .Remove(prevMacro)
                    .Add(macro)
            });

            var colorProgram = matRefs.ColorProgram;
            var depthProgram = matRefs.DepthProgram;

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                state.RenderMode = mode;
                state.ColorProgram = colorProgram;
                state.DepthProgram = depthProgram;
                return true;
            });
        });

        Listen((EntityRef entity, ref Material snapshot, in Material.SetLightingMode cmd) => {
            var mode = cmd.Value;
            ref var matRefs = ref entity.GetState<MaterialReferences>();

            RecreateShaderPrograms(world, entity, ref matRefs, matRefs.ColorProgramAsset with {
                Macros = matRefs.ColorProgramAsset.Macros
                    .Remove("LightingMode_" + Enum.GetName(snapshot.LightingMode))
                    .Add("LightingMode_" + Enum.GetName(mode))
            });

            var colorProgram = matRefs.ColorProgram;
            var depthProgram = matRefs.DepthProgram;

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                state.LightingMode = mode;
                state.ColorProgram = colorProgram;
                state.DepthProgram = depthProgram;
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetIsTwoSided cmd) => {
            var value = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                state.IsTwoSided = value;
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetShaderProgram cmd) => {
            ref var material = ref entity.Get<Material>();
            ref var matRefs = ref entity.GetState<MaterialReferences>();

            RecreateShaderPrograms(world, entity, ref matRefs,
                TransformMaterialShaderProgramAsset(material));

            var colorProgram = matRefs.ColorProgram;
            var depthProgram = matRefs.DepthProgram;

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                state.ColorProgram = colorProgram;
                state.DepthProgram = depthProgram;
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetProperties cmd) => {
            var props = cmd.Value;
            ref var matRefs = ref entity.GetState<MaterialReferences>();
            ref var textures = ref matRefs.Textures;

            if (textures != null) {
                foreach (var texture in textures.Values) {
                    entity.UnreferAsset(texture);
                }
                textures.Clear();
            }

            List<(string, EntityRef)>? newTextures = null;

            if (props.Count != 0) {
                foreach (var (propName, dyn) in props) {
                    if (TryLoadTexture(entity, propName, dyn, out var texEntity)) {
                        newTextures ??= [];
                        newTextures.Add((propName, texEntity));
                    }
                }
            }

            if (newTextures != null) {
                textures ??= [];
                foreach (var (propName, texEntity) in newTextures.AsSpan()) {
                    textures.Add(propName, texEntity);
                }
            }

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();

                var textures = state.Textures;
                textures?.Clear();

                if (newTextures != null) {
                    textures ??= [];
                    foreach (var (propName, texEntity) in newTextures.AsSpan()) {
                        textures.Add(propName, texEntity);
                    }
                }

                var colorProgram = state.ColorProgram;
                var pointer = state.Pointer;

                ref var programState = ref colorProgram.GetState<GLSLProgramState>();
                if (!programState.Loaded) {
                    return false;
                }
                unsafe {
                    new Span<byte>((void*)pointer, programState.MaterialBlockSize).Clear();
                }
                SetMaterialParameters(entity, pointer, programState, props);
                return true;
            });
        });

        void SetProperty(EntityRef entity, string name, Dyn value)
        {
            ref var matRefs = ref entity.GetState<MaterialReferences>();
            var textures = matRefs.Textures;

            if (textures != null && textures.Remove(name, out var prevTexEntity)) {
                entity.UnreferAsset(prevTexEntity);
            }

            bool isTexture = false;
            if (TryLoadTexture(entity, name, value, out var texEntity)) {
                textures ??= [];
                textures.Add(name, texEntity);
                isTexture = true;
            }

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                ref var programState = ref state.ColorProgram.GetState<GLSLProgramState>();

                if (!programState.Loaded) {
                    return false;
                }
                if (isTexture) {
                    state.Textures ??= [];
                    state.Textures[name] = texEntity;
                }
                SetMaterialParameter(
                    entity, state.Pointer, programState.Parameters, name, value);
                return true;
            });
        }

        Listen((EntityRef entity, in Material.AddProperty cmd) => SetProperty(entity, cmd.Key, cmd.Value));
        Listen((EntityRef entity, in Material.SetProperty cmd) => SetProperty(entity, cmd.Key, cmd.Value));
        Listen((EntityRef entity, in Material.RemoveProperty cmd) => {
            var name = cmd.Key;

            ref var matRefs = ref entity.GetState<MaterialReferences>();
            var textures = matRefs.Textures;

            bool isTexture = false;
            if (textures != null && textures.Remove(name, out var texEntity)) {
                entity.UnreferAsset(texEntity);
                isTexture = true;
            }

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<MaterialState>();
                ref var programState = ref state.ColorProgram.GetState<GLSLProgramState>();

                if (programState.Handle == ProgramHandle.Zero) {
                    return false;
                }
                
                if (isTexture) {
                    state.Textures!.Remove(name);
                }

                ClearMaterialParameter(
                    entity, state.Pointer, programState.Parameters, name);
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Material asset, EntityRef stateEntity)
    {
        Dictionary<string, EntityRef>? textures = null;

        var colorProgramAsset = TransformMaterialShaderProgramAsset(
            asset, (world, name, value) => {
                if (TryLoadTexture(entity, name, value, out var texEntity)) {
                    textures ??= [];
                    textures.Add(name, texEntity);
                }
            }
        );
        var depthProgramAsset = CreateDepthShaderProgramAsset(colorProgramAsset);

        var programManager = World.GetAddon<GLSLProgramManager>();
        var colorProgram = programManager.Acquire(colorProgramAsset, entity);
        var depthProgram = programManager.Acquire(depthProgramAsset, entity);

        ref var matRefs = ref stateEntity.Get<MaterialReferences>();
        matRefs.Textures = textures;
        matRefs.ColorProgram = colorProgram;
        matRefs.ColorProgramAsset = colorProgramAsset;
        matRefs.DepthProgram = depthProgram;
        matRefs.DepthProgramAsset = depthProgramAsset;

        var name = asset.Name;
        var renderMode = asset.RenderMode;
        var lightingMode = asset.LightingMode;
        var isTwoSided = asset.IsTwoSided;
        var properties = asset.Properties;
        var stateTextures = textures?.ToDictionary();

        RenderFrame.Enqueue(entity, () => {
            ref var programState = ref colorProgram.GetState<GLSLProgramState>();
            if (!programState.Loaded) {
                return false;
            }

            ref var state = ref stateEntity.Get<MaterialState>();
            state = new MaterialState {
                ColorProgram = colorProgram,
                DepthProgram = depthProgram,
                UniformBufferHandle = new(GL.GenBuffer()),
                RenderMode = renderMode,
                LightingMode = lightingMode,
                IsTwoSided = isTwoSided,
                Textures = stateTextures
            };

            GL.BindBuffer(BufferTargetARB.UniformBuffer, state.UniformBufferHandle.Handle);
            state.Pointer = GLUtils.InitializeBuffer(
                BufferTargetARB.UniformBuffer, programState.MaterialBlockSize);

            SetMaterialParameters(entity, state, programState, properties);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Material asset, EntityRef stateEntity)
    {
        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<MaterialState>();
            GL.DeleteBuffer(state.UniformBufferHandle.Handle);
            return true;
        });
    }

    private void SetMaterialParameters(
        in EntityRef entity, in MaterialState state, in GLSLProgramState programState, ImmutableDictionary<string, Dyn> properties)
        => SetMaterialParameters(entity, state.Pointer, programState, properties);

    private void SetMaterialParameters(
        in EntityRef entity, nint pointer, in GLSLProgramState programState, ImmutableDictionary<string, Dyn> properties)
    {
        var pars = programState.Parameters;
        foreach (var (propName, value) in properties) {
            SetMaterialParameter(entity, pointer, pars, propName, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetMaterialParameter(
        in EntityRef entity, nint pointer, Dictionary<string, ShaderParameterEntry>? parameters, string propName, Dyn value)
    {
        if (parameters == null || !parameters.TryGetValue(propName, out var entry)) {
            Logger.LogWarning("[{Name}] Unrecognized property '{Property}' in material, skip.", entity.GetDisplayName(), propName);
            return;
        }
        if (!ShaderUtils.SetParameter(pointer + entry.Offset, entry.Type, value)) {
            Logger.LogError("[{Name}] Parameter '{Parameter}' requires type {ParameterType} that does not match with actual type {ActualType}.",
                entity.GetDisplayName(), propName, Enum.GetName(entry.Type), value.GetType());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearMaterialParameter(
        in EntityRef entity, nint pointer, Dictionary<string, ShaderParameterEntry>? parameters, string propName)
    {
        if (parameters == null || !parameters.TryGetValue(propName, out var entry)) {
            Logger.LogWarning("[{Name}] Unrecognized property '{Property}' in material, skip.", entity.GetDisplayName(), propName);
            return;
        }
        ShaderUtils.ClearParameter(pointer + entry.Offset, entry.Type);
    }

    private bool TryLoadTexture(in EntityRef entity, string propName, Dyn dyn, out EntityRef resultTexEntity)
    {
        if (dyn is not TextureDyn textureDyn || textureDyn.Value == null) {
            resultTexEntity = default;
            return false;
        }
        try {
            resultTexEntity = textureDyn.Value switch {
                Texture2DAsset texture =>
                    Texture2D.CreateEntity(World, texture, entity),
                CubemapAsset cubemap =>
                    Cubemap.CreateEntity(World, cubemap, entity),
                RenderTexture2DAsset renderTexture =>
                    RenderTexture2D.CreateEntity(World, renderTexture, entity),
                _ => throw new NotSupportedException("Texture not supported")
            };
        }
        catch (Exception e) {
            Logger.LogError("[{Name}] Failed to create texture entity for property '{Property}': {Message}",
                entity.GetDisplayName(), propName, e.Message);
            resultTexEntity = default;
            return false;
        }
        return true;
    }

    public static GLSLProgramAsset CreateDepthShaderProgramAsset(GLSLProgramAsset colorProgramAsset)
        => colorProgramAsset.WithShader(ShaderType.Fragment, ShaderUtils.EmptyFragmentShader);

    public GLSLProgramAsset TransformMaterialShaderProgramAsset(
        in Material material, Action<World, string, Dyn>? propertyHandler = null)
    {
        var program = material.ShaderProgram;
        var renderMode = material.RenderMode;
        var lightingMode = material.LightingMode;
        var props = material.Properties;

        var macros = program.Macros.ToBuilder();
        macros.Add("RenderMode_" + Enum.GetName(renderMode));
        macros.Add("LightingMode_" + Enum.GetName(lightingMode));
        
        if (props.Count == 0) {
            return program with { Macros = macros.ToImmutable() };
        }

        var programPars = program.Parameters;
        if (propertyHandler != null) {
            foreach (var (name, value) in props) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);

                try {
                    propertyHandler(World, name, value);
                }
                catch (Exception e) {
                    Logger.LogError("Uncaught execption when handling material property: {Message}", e);
                }
            }
        }
        else {
            foreach (var name in props.Keys) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);
            }
        }

        return program with { Macros = macros.ToImmutable() };
    }
}