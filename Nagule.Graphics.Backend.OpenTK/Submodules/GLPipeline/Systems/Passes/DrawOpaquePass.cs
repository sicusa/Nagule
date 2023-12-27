namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawOpaquePass : DrawPassBase
{
    public bool DepthMask { get; init; } = false;

    public DrawOpaquePass() : base(MeshFilter.Opaque) {}

    protected override void BeginPass()
    {
        if (!DepthMask) {
            GL.DepthMask(true);
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

    protected override void Draw(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.BindVertexArray(group.VertexArrayHandle.Handle);
        GL.DrawElementsInstanced(
            meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, group.Count);
    }
}