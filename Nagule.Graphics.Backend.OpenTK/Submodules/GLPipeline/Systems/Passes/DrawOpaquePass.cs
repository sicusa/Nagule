namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawOpaquePass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaque)
{
    public bool DepthMask { get; init; } = true;

    protected override void BeginPass()
    {
        if (!DepthMask) {
            GL.DepthMask(false);
        }
    }

    protected override void EndPass()
    {
        if (!DepthMask) {
            GL.DepthMask(true);
        }
    }

    protected override EntityRef GetShaderProgram(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgram;

    protected override int Draw(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.BindVertexArray(group.VertexArrayHandle.Handle);
        GL.DrawElementsInstanced(
            meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, group.Count);
        return group.Count;
    }
}