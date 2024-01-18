namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DrawDepthPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaqueOrCutoff)
{
    protected override void BeginPass()
        => GL.ColorMask(false, false, false, false);

    protected override void EndPass()
        => GL.ColorMask(true, true, true, true);

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.DepthProgramState;
}