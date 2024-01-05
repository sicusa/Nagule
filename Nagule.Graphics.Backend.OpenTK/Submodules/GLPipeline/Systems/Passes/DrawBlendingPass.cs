namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawBlendingPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsBlending)
{
    protected override void BeginPass()
    {
        GL.Enable(EnableCap.Blend);
    }

    protected override void EndPass()
    {
        GL.Disable(EnableCap.Blend);
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