namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sia;

public class MaterialManager : GraphicsAssetManagerBase<Material, MaterialAsset, MaterialState>
{
    private class Data
    {
        public ImmutableDictionary<string, EntityRef> Textures =
            ImmutableDictionary<string, EntityRef>.Empty;
    }

    [AllowNull] private GLSLProgramManager _programManager;

    private readonly Dictionary<EntityRef, Data> _dataDict = [];

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _programManager = world.GetAddon<GLSLProgramManager>();
    
        static void RecreateShaderPrograms(
            MaterialManager manager, in EntityRef entity, ref MaterialState state, GLSLProgramAsset colorProgramAsset)
        {
            var world = manager.World;
            var programManager = manager._programManager;

            entity.UnreferAsset(state.ColorProgram);
            entity.UnreferAsset(state.DepthProgram);

            state.ColorProgramAsset = colorProgramAsset;
            state.DepthProgramAsset = CreateDepthShaderProgramAsset(colorProgramAsset);

            state.ColorProgram = programManager.Acquire(state.ColorProgramAsset, entity);
            state.DepthProgram = programManager.Acquire(state.DepthProgramAsset, entity);
        }

        Listen((EntityRef entity, ref Material snapshot, in Material.SetRenderMode cmd) => {
            var mode = cmd.Value;
            var prevMode = snapshot.RenderMode;

            var macro = "RenderMode_" + Enum.GetName(mode);
            var prevMacro = "RenderMode_" + Enum.GetName(prevMode);

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                var colorProgramAsset = state.ColorProgramAsset with {
                    Macros = state.ColorProgramAsset.Macros
                        .Remove(prevMacro)
                        .Add(macro)
                };
                RecreateShaderPrograms(this, entity, ref state, colorProgramAsset);
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetLightingMode cmd) => {
            var mode = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);

                var prevMode = state.LightingMode;
                state.LightingMode = mode;

                var colorProgramAsset = state.ColorProgramAsset with {
                    Macros = state.ColorProgramAsset.Macros
                        .Remove("LightingMode_" + Enum.GetName(prevMode))
                        .Add("LightingMode_" + Enum.GetName(mode))
                };

                RecreateShaderPrograms(this, entity, ref state, colorProgramAsset);
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetIsTwoSided cmd) => {
            var value = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                RenderStates.Get(entity).IsTwoSided = value;
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetShaderProgram cmd) => {
            ref var material = ref entity.Get<Material>();
            var colorProgramAsset = TransformMaterialShaderProgramAsset(material);

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                RecreateShaderPrograms(this, entity, ref state, colorProgramAsset);
                return true;
            });
        });

        Listen((EntityRef entity, in Material.SetProperties cmd) => {
            var props = cmd.Value;

            var data = _dataDict[entity];
            var textures = data.Textures;

            foreach (var texture in textures.Values) {
                entity.UnreferAsset(texture);
            }

            if (props.Count != 0) {
                ImmutableDictionary<string, EntityRef>.Builder? builder = null;
                foreach (var (propName, dyn) in props) {
                    if (TryLoadTexture(entity, propName, dyn, out var texEntity)) {
                        builder ??= ImmutableDictionary.CreateBuilder<string, EntityRef>();
                        builder.Add(propName, texEntity);
                    }
                }
                textures = builder != null
                    ? builder.ToImmutable()
                    : ImmutableDictionary<string, EntityRef>.Empty;
            }
            else {
                textures = ImmutableDictionary<string, EntityRef>.Empty;
            }

            data.Textures = textures;

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                state.Textures = textures;

                var colorProgram = state.ColorProgram;
                var pointer = state.Pointer;

                ref var programState = ref _programManager.RenderStates.GetOrNullRef(colorProgram);
                if (Unsafe.IsNullRef(ref programState)) {
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
            var data = _dataDict[entity];
            var textures = data.Textures;

            if (textures.TryGetValue(name, out var prevTexEntity)) {
                entity.UnreferAsset(prevTexEntity);
                textures = textures.Remove(name);
            }
            if (TryLoadTexture(entity, name, value, out var texEntity)) {
                textures = textures.Add(name, texEntity);
            }
            data.Textures = textures;

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                ref var programState = ref _programManager.RenderStates.GetOrNullRef(state.ColorProgram);
                if (Unsafe.IsNullRef(ref programState)) {
                    return false;
                }
                state.Textures = textures;
                SetMaterialParameter(
                    entity, state.Pointer, programState.Parameters, name, value);
                return true;
            });
        }

        Listen((EntityRef entity, in Material.AddProperty cmd) => SetProperty(entity, cmd.Key, cmd.Value));
        Listen((EntityRef entity, in Material.SetProperty cmd) => SetProperty(entity, cmd.Key, cmd.Value));
        Listen((EntityRef entity, in Material.RemoveProperty cmd) => {
            var name = cmd.Key;

            var data = _dataDict[entity];
            var textures = data.Textures;

            if (textures.TryGetValue(name, out var texEntity)) {
                entity.UnreferAsset(texEntity);
                textures = textures.Remove(name);
                data.Textures = textures;
            }

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                ref var programState = ref _programManager.RenderStates.GetOrNullRef(state.ColorProgram);
                if (Unsafe.IsNullRef(ref programState)) {
                    return false;
                }
                state.Textures = textures;
                ClearMaterialParameter(
                    entity, state.Pointer, programState.Parameters, name);
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Material asset)
    {
        ImmutableDictionary<string, EntityRef>.Builder? texturesBuilder = null;

        var colorProgramAsset = TransformMaterialShaderProgramAsset(
            asset, (world, name, value) => {
                if (TryLoadTexture(entity, name, value, out var texEntity)) {
                    texturesBuilder ??= ImmutableDictionary.CreateBuilder<string, EntityRef>();
                    texturesBuilder.Add(name, texEntity);
                }
            }
        );
        var depthProgramAsset = CreateDepthShaderProgramAsset(colorProgramAsset);

        var colorShaderProgram = _programManager.Acquire(colorProgramAsset, entity);
        var depthShaderProgram = _programManager.Acquire(depthProgramAsset, entity);

        var data = new Data();
        if (texturesBuilder != null) {
            data.Textures = texturesBuilder.ToImmutable();
        }
        _dataDict[entity] = data;

        var name = asset.Name;
        var renderMode = asset.RenderMode;
        var lightingMode = asset.LightingMode;
        var isTwoSided = asset.IsTwoSided;
        var properties = asset.Properties;
        var textures = data.Textures;

        RenderFrame.Enqueue(entity, () => {
            ref var programState = ref _programManager.RenderStates.GetOrNullRef(colorShaderProgram);
            if (Unsafe.IsNullRef(ref programState)) {
                return false;
            }

            var state = new MaterialState {
                ColorProgramAsset = colorProgramAsset,
                ColorProgram = colorShaderProgram,
                DepthProgramAsset = depthProgramAsset,
                DepthProgram = depthShaderProgram,
                UniformBufferHandle = new(GL.GenBuffer()),
                RenderMode = renderMode,
                LightingMode = lightingMode,
                IsTwoSided = isTwoSided,
                Textures = textures
            };

            GL.BindBuffer(BufferTargetARB.UniformBuffer, state.UniformBufferHandle.Handle);
            state.Pointer = GLUtils.InitializeBuffer(
                BufferTargetARB.UniformBuffer, programState.MaterialBlockSize);

            SetMaterialParameters(entity, state, programState, properties);
            RenderStates.Set(entity, state);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Material asset)
    {
        RenderFrame.Enqueue(entity, () => {
            if (RenderStates.Remove(entity, out var state)) {
                GL.DeleteBuffer(state.UniformBufferHandle.Handle);
            }
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
            Logger.LogWarning("[{Name}] Unrecognized property '{Property}' in material, skip.", entity.GetName(), propName);
            return;
        }
        if (!ShaderUtils.SetParameter(pointer + entry.Offset, entry.Type, value)) {
            Logger.LogError("[{Name}] Parameter '{Parameter}' requires type {ParameterType} that does not match with actual type {ActualType}.",
                entity.GetName(), propName, Enum.GetName(entry.Type), value.GetType());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearMaterialParameter(
        in EntityRef entity, nint pointer, Dictionary<string, ShaderParameterEntry>? parameters, string propName)
    {
        if (parameters == null || !parameters.TryGetValue(propName, out var entry)) {
            Logger.LogWarning("[{Name}] Unrecognized property '{Property}' in material, skip.", entity.GetName(), propName);
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
                entity.GetName(), propName, e.Message);
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