namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DrawDepthPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaqueOrCutoff)
{
    protected override void BeginPass()
    {
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
    }

    protected override void EndPass()
    {
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
    }

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.DepthProgramState!.Value;
}