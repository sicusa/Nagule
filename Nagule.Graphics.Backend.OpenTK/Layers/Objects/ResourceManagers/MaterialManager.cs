namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>, IDeferrableCommand
    {
        public Guid MaterialId;
        public Material? Resource;
        public Guid ShaderProgramId;
        public Guid DepthShaderProgramId;
        [AllowNull] public Dictionary<string, Guid> Textures;

        public override Guid? Id => MaterialId;

        public bool ShouldExecute(ICommandHost host)
            => host.Contains<GLSLProgramData>(ShaderProgramId);

        public unsafe override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<MaterialData>(MaterialId, out bool exists);
            ref readonly var programData = ref host.Inspect<GLSLProgramData>(ShaderProgramId);

            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(
                    BufferTargetARB.UniformBuffer, programData.MaterialBlockSize);
            }

            data.IsTwoSided = Resource!.IsTwoSided;
            data.ShaderProgramId = ShaderProgramId;
            data.DepthShaderProgramId = DepthShaderProgramId;
            data.Textures = Textures;

            if (programData.Parameters != null) {
                foreach (var (name, value) in Resource.Properties) {
                    SetParameter(in data, in programData, name, value);
                }
            }
        }

        private void SetParameter(in MaterialData data, in GLSLProgramData programData, string name, Dyn value)
        {
            if (!programData.Parameters!.TryGetValue(name, out var entry)) {
                return;
            }

            bool success = true;

            switch (entry.Type) {
            case ShaderParameterType.Unit:
                success = value is Dyn.Unit;
                break;
            case ShaderParameterType.Texture:
                success = value is TextureDyn;
                break;
            default:
                try {
                    var setter = s_propertySetters[entry.Type];
                    int offset = entry.Offset;
                    setter(data.Pointer + offset, value);
                }
                catch {
                    success = false;
                }
                break;
            }

            if (!success) {
                Console.WriteLine(
                    $"Error: parameter '{name}' has type {Enum.GetName(entry.Type)} that does not match with argument type " + value.GetType());
            }
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

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    private unsafe static EnumArray<ShaderParameterType, Action<IntPtr, Dyn>> s_propertySetters = new() {
        [ShaderParameterType.Int] = (ptr, dyn) => *(int*)ptr = ((Dyn.Int)dyn).Value,
        [ShaderParameterType.UInt] = (ptr, dyn) => *(uint*)ptr = ((Dyn.UInt)dyn).Value,
        [ShaderParameterType.Bool] = (ptr, dyn) => *(bool*)ptr = ((Dyn.Bool)dyn).Value,
        [ShaderParameterType.Float] = (ptr, dyn) => *(float*)ptr = ((Dyn.Float)dyn).Value,
        [ShaderParameterType.Double] = (ptr, dyn) => *(double*)ptr = ((Dyn.Double)dyn).Value,

        [ShaderParameterType.Vector2] = (ptr, dyn) => *(Vector2*)ptr = ((Dyn.Vector2)dyn).Value,
        [ShaderParameterType.Vector3] = (ptr, dyn) => *(Vector3*)ptr = ((Dyn.Vector3)dyn).Value,
        [ShaderParameterType.Vector4] = (ptr, dyn) => *(Vector4*)ptr = ((Dyn.Vector4)dyn).Value,

        [ShaderParameterType.DoubleVector2] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.DoubleVector3] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.DoubleVector4] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.IntVector2] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.IntVector3] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.IntVector4] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.UIntVector2] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.UIntVector3] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.UIntVector4] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.Matrix4x4] = (ptr, dyn) =>
            *(Matrix4x4*)ptr = ((Dyn.Matrix4x4)dyn).Value,
        [ShaderParameterType.Matrix4x3] = (ptr, dyn) =>
            ((Dyn.Matrix4x3)dyn).Value.CopyTo(new Span<float>((float*)ptr, 12)),
        [ShaderParameterType.Matrix3x3] = (ptr, dyn) =>
            ((Dyn.Matrix3x3)dyn).Value.CopyTo(new Span<float>((float*)ptr, 9)),
        [ShaderParameterType.Matrix3x2] = (ptr, dyn) =>
            *(Matrix3x2*)ptr = ((Dyn.Matrix3x2)dyn).Value,
        [ShaderParameterType.Matrix2x2] = (ptr, dyn) =>
            ((Dyn.Matrix2x2)dyn).Value.CopyTo(new Span<float>((float*)ptr, 4)),

        [ShaderParameterType.DoubleMatrix4x4] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix4x4)dyn).Value.CopyTo(new Span<double>((double*)ptr, 16)),
        [ShaderParameterType.DoubleMatrix4x3] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix4x3)dyn).Value.CopyTo(new Span<double>((double*)ptr, 12)),
        [ShaderParameterType.DoubleMatrix3x3] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix3x3)dyn).Value.CopyTo(new Span<double>((double*)ptr, 9)),
        [ShaderParameterType.DoubleMatrix3x2] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix3x2)dyn).Value.CopyTo(new Span<double>((double*)ptr, 6)),
        [ShaderParameterType.DoubleMatrix2x2] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix2x2)dyn).Value.CopyTo(new Span<double>((double*)ptr, 4)),
    };

    protected override void Initialize(
        IContext context, Guid id, Material resource, Material? prevResource)
    {
        if (prevResource != null) {
            UnreferenceDependencies(context, id);
        }

        var cmd = InitializeCommand.Create();
        cmd.MaterialId = id;
        cmd.Resource = resource;

        var programResource = resource.ShaderProgram ??
            context.Inspect<Resource<GLSLProgram>>(Graphics.DefaultShaderProgramId).Value;
        
        var macros = programResource.Macros.ToBuilder();
        macros.Add("RenderMode_" + Enum.GetName(resource.RenderMode));
        macros.Add("LightingMode_" + Enum.GetName(resource.LightingMode));
        
        var props = resource.Properties;
        if (props.Count != 0) {
            var programPars = programResource.Parameters;
            foreach (var (name, value) in props) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);
                if (value is TextureDyn textureDyn) {
                    cmd.Textures ??= new();
                    cmd.Textures[name] = ResourceLibrary<Texture>.Reference(context, id, textureDyn.Value);
                }
            }
        }

        programResource = programResource with {Macros = macros.ToImmutable() };

        cmd.ShaderProgramId =
            ResourceLibrary<GLSLProgram>.Reference(context, id, programResource);

        cmd.DepthShaderProgramId =
            ResourceLibrary<GLSLProgram>.Reference(
                context, id, programResource.WithShader(ShaderType.Fragment, EmptyFragmentShader));

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Material resource)
    {
        UnreferenceDependencies(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.MaterialId = id;
        context.SendCommandBatched(cmd);
    }

    private void UnreferenceDependencies(IContext context, Guid id)
    {
        ResourceLibrary<GLSLProgram>.UnreferenceAll(context, id);
        ResourceLibrary<Texture>.UnreferenceAll(context, id);
    }
}