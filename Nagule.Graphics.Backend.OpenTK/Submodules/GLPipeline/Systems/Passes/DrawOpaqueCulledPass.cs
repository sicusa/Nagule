namespace Nagule.Graphics.Backend.OpenTK;

public class DrawOpaqueCulledPass : DrawOpaquePass
{
    private int _visibleCount;

    protected override bool BeforeDraw(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.GetQueryObjecti(group.CulledQueryHandle.Handle, QueryObjectParameterName.QueryResult, ref _visibleCount);
        return _visibleCount != 0;
    }

    protected override void Draw(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.BindVertexArray(group.CulledVertexArrayHandle.Handle);
        GL.DrawElementsInstanced(
            meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, _visibleCount);
    }
}