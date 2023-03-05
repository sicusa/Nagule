namespace Nagule.Graphics.Backend.OpenTK;

using System.Reactive.Disposables;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>, IDeferrableCommand
    {
        public Guid MaterialId;
        public Material? Resource;
        public Guid ColorShaderProgramId;
        public Guid DepthShaderProgramId;
        public Dictionary<string, Guid>? Textures;

        public override Guid? Id => MaterialId;

        public bool ShouldExecute(ICommandHost host)
            => host.Contains<GLSLProgramData>(ColorShaderProgramId);

        public unsafe override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<MaterialData>(MaterialId, out bool exists);
            ref readonly var programData = ref host.Inspect<GLSLProgramData>(ColorShaderProgramId);

            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(
                    BufferTargetARB.UniformBuffer, programData.MaterialBlockSize);
            }

            data.RenderMode = Resource!.RenderMode;
            data.IsTwoSided = Resource.IsTwoSided;
            data.ColorShaderProgramId = ColorShaderProgramId;
            data.DepthShaderProgramId = DepthShaderProgramId;
            data.Textures = Textures;

            if (programData.Parameters != null) {
                var pars = programData.Parameters;
                var ptr = data.Pointer;
                
                foreach (var (name, value) in Resource.Properties) {
                    if (pars.TryGetValue(name, out var entry)) {
                        GraphicsHelper.SetShaderParameter(
                            name, entry.Type, value, ptr + entry.Offset);
                    }
                }
            }

            host.Acquire<MeshGroupDirty>();
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid MaterialId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<MaterialData>(MaterialId, out var data)) {
                GL.DeleteBuffer(data.Handle);
            }
        }
    }

    protected override void Initialize(
        IContext context, Guid id, Material resource, Material? prevResource)
    {
        if (prevResource != null) {
            ResourceLibrary.UnreferenceAll(context, id);
        }

        Material.GetProps(context, id).Set(resource);

        var cmd = InitializeCommand.Create();
        cmd.MaterialId = id;
        cmd.Resource = resource;

        var shaderProgram = TransformMaterialShaderProgram(
            context, resource, (context, name, value) => {
                switch (value) {
                case TextureDyn textureDyn when textureDyn.Value != null:
                    cmd.Textures ??= new();
                    cmd.Textures[name] = ResourceLibrary.Reference(context, id, textureDyn.Value);
                    break;
                case CubemapDyn cubemapDyn when cubemapDyn.Value != null:
                    cmd.Textures ??= new();
                    cmd.Textures[name] = ResourceLibrary.Reference(context, id, cubemapDyn.Value);
                    break;
                case RenderTextureDyn renderTexDyn when renderTexDyn.Value != null:
                    cmd.Textures ??= new();
                    cmd.Textures[name] = ResourceLibrary.Reference(context, id, renderTexDyn.Value);
                    break;
                }
            });
        
        ref var programs = ref context.Acquire<MaterialShaderPrograms>(id);
        programs.Color = shaderProgram;
        programs.Depth = shaderProgram.WithShader(
            ShaderType.Fragment, GraphicsHelper.EmptyFragmentShader);

        cmd.ColorShaderProgramId = ResourceLibrary.Reference(context, id, programs.Color);
        cmd.DepthShaderProgramId = ResourceLibrary.Reference(context, id, programs.Depth);

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable? Subscribe(IContext context, Guid id, Material resource)
    {
        var props = Material.GetProps(context, id);

        return new CompositeDisposable(
            props.RenderMode.Modified.Subscribe(tuple => {
                var (prevMode, mode) = tuple;
                
                ref var programs = ref context.Require<MaterialShaderPrograms>(id);
                ResourceLibrary.Unreference(context, id, programs.Color);
                ResourceLibrary.Unreference(context, id, programs.Depth);

                var prevRenderModeMacro = "RenderMode_" + Enum.GetName(prevMode);
                var renderModeMacro = "RenderMode_" + Enum.GetName(mode);

                programs.Color = programs.Color with {
                    Macros = programs.Color.Macros
                        .Remove(prevRenderModeMacro)
                        .Add(renderModeMacro)
                };
                programs.Depth = programs.Depth with {
                    Macros = programs.Color.Macros
                };

                var colorProgramId = ResourceLibrary.Reference(context, id, programs.Color);
                var depthProgramId = ResourceLibrary.Reference(context, id, programs.Depth);
                bool hasReferencers = context.TryGet<ResourceReferencers>(id, out var referencers)
                    && referencers.Ids.Count != 0;
                
                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<MaterialData>(id);
                    data.RenderMode = mode;
                    data.ColorShaderProgramId = colorProgramId;
                    data.DepthShaderProgramId = depthProgramId;

                    if (hasReferencers) {
                        host.Acquire<MeshGroupDirty>();
                    }
                }));
            }),

            props.LightingMode.Modified.Subscribe(tuple => {
                var (prevMode, mode) = tuple;
                
                ref var programs = ref context.Require<MaterialShaderPrograms>(id);
                ResourceLibrary.Unreference(context, id, programs.Color);
                ResourceLibrary.Unreference(context, id, programs.Depth);

                var prevLightingModeMacro = "LightingMode_" + Enum.GetName(prevMode);
                var lightingModeMacro = "LightingMode_" + Enum.GetName(mode);

                programs.Color = programs.Color with {
                    Macros = programs.Color.Macros
                        .Remove(prevLightingModeMacro)
                        .Add(lightingModeMacro)
                };
                programs.Depth = programs.Depth with {
                    Macros = programs.Color.Macros
                };

                var colorProgramId = ResourceLibrary.Reference(context, id, programs.Color);
                var depthProgramId = ResourceLibrary.Reference(context, id, programs.Depth);
                
                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<MaterialData>(id);
                    data.ColorShaderProgramId = colorProgramId;
                    data.DepthShaderProgramId = depthProgramId;
                }));
            }),

            props.IsTwoSided.SubscribeCommand<bool, RenderTarget>(
                context, (host, value) => {
                    ref var data = ref host.Require<MaterialData>(id);
                    data.IsTwoSided = value;
                }),
            
            props.ShaderProgram.Subscribe(program => {
                ref var programs = ref context.Require<MaterialShaderPrograms>(id);
                ResourceLibrary.Unreference(context, id, programs.Color);
                ResourceLibrary.Unreference(context, id, programs.Depth);

                programs.Color = TransformMaterialShaderProgram(
                    context, program, props.RenderMode.Value, props.LightingMode.Value, props.Properties);
                programs.Depth = programs.Color.WithShader(
                    ShaderType.Fragment, GraphicsHelper.EmptyFragmentShader);
                
                var colorProgramId = ResourceLibrary.Reference(context, id, programs.Color);
                var depthProgramId = ResourceLibrary.Reference(context, id, programs.Depth);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<MaterialData>(id);
                    data.ColorShaderProgramId = colorProgramId;
                    data.DepthShaderProgramId = depthProgramId;
                }));
            }),

            props.Properties.Subscribe(e => {
                switch (e.Operation) {
                case ReactiveDictionaryOperation.Set:
                    Guid? texId = null;
                    switch (e.Value) {
                    case TextureDyn textureDyn when textureDyn.Value != null:
                        texId = ResourceLibrary.Reference(context, id, textureDyn.Value);
                        break;
                    case CubemapDyn cubemapDyn when cubemapDyn.Value != null:
                        texId = ResourceLibrary.Reference(context, id, cubemapDyn.Value);
                        break;
                    case RenderTextureDyn renderTexDyn when renderTexDyn.Value != null:
                        texId = ResourceLibrary.Reference(context, id, renderTexDyn.Value);
                        break;
                    }
                    context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                        ref var data = ref host.Require<MaterialData>(id);
                        if (texId.HasValue) {
                            data.Textures ??= new();
                            data.Textures[e.Key] = texId.Value;
                        }
                        else {
                            ref readonly var programData = ref host.Inspect<GLSLProgramData>(data.ColorShaderProgramId);
                            var pars = programData.Parameters;
                            if (pars != null && pars.TryGetValue(e.Key, out var entry)) {
                                GraphicsHelper.SetShaderParameter(
                                    e.Key, entry.Type, e.Value, data.Pointer + entry.Offset);
                            }
                        }
                    }));
                    break;

                case ReactiveDictionaryOperation.Remove:
                    bool isTexture = false;
                    switch (e.Value) {
                    case TextureDyn textureDyn when textureDyn.Value != null:
                        ResourceLibrary.Unreference(context, id, textureDyn.Value);
                        isTexture = true;
                        break;
                    case CubemapDyn cubemapDyn when cubemapDyn.Value != null:
                        ResourceLibrary.Unreference(context, id, cubemapDyn.Value);
                        isTexture = true;
                        break;
                    case RenderTextureDyn renderTexDyn when renderTexDyn.Value != null:
                        ResourceLibrary.Unreference(context, id, renderTexDyn.Value);
                        isTexture = true;
                        break;
                    }

                    if (isTexture) {
                        context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                            ref var data = ref host.Require<MaterialData>(id);
                            data.Textures?.Remove(e.Key);
                        }));
                    }
                    else {
                        ref var programs = ref context.Require<MaterialShaderPrograms>(id);
                        ResourceLibrary.Unreference(context, id, programs.Color);
                        ResourceLibrary.Unreference(context, id, programs.Depth);

                        programs.Color = programs.Color with {
                            Macros = programs.Color.Macros.Remove("_" + e.Key),
                            Parameters = programs.Color.Parameters.Remove(e.Key)
                        };
                        programs.Depth = programs.Depth with {
                            Macros = programs.Color.Macros,
                            Parameters = programs.Color.Parameters
                        };

                        var colorProgramId = ResourceLibrary.Reference(context, id, programs.Color);
                        var depthProgramId = ResourceLibrary.Reference(context, id, programs.Depth);
                        
                        context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                            ref var data = ref host.Require<MaterialData>(id);
                            data.ColorShaderProgramId = colorProgramId;
                            data.DepthShaderProgramId = depthProgramId;
                        }));
                    }
                    break;
                }
            })
        );
    }

    protected override void Uninitialize(IContext context, Guid id, Material resource)
    {
        ResourceLibrary.UnreferenceAll(context, id);
        context.Remove<MaterialShaderPrograms>(id);

        var cmd = UninitializeCommand.Create();
        cmd.MaterialId = id;
        context.SendCommandBatched(cmd);
    }

    public static GLSLProgram TransformMaterialShaderProgram(
        IContext context, Material material, Action<IContext, string, Dyn>? propertyHandler = null)
        => TransformMaterialShaderProgram(context,
                material.ShaderProgram,
                material.RenderMode,
                material.LightingMode,
                material.Properties,
                propertyHandler);

    public static GLSLProgram TransformMaterialShaderProgram(
        IContext context, GLSLProgram program, RenderMode renderMode, LightingMode lightingMode,
        IReadOnlyDictionary<string, Dyn> props, Action<IContext, string, Dyn>? propertyHandler = null)
    {
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
                propertyHandler(context, name, value);
            }
        }
        else {
            foreach (var (name, value) in props) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);
            }
        }

        return program with { Macros = macros.ToImmutable() };
    }
}