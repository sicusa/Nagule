namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawDepthPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaque)
{
    protected override void BeginPass()
        => GL.ColorMask(false, false, false, false);

    protected override void EndPass()
        => GL.ColorMask(true, true, true, true);

    protected override EntityRef GetShaderProgram(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.DepthProgram;

    protected override int Draw(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.BindVertexArray(group.VertexArrayHandle.Handle);
        GL.DrawElementsInstanced(
            meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, group.Count);
        return group.Count;
    }
}