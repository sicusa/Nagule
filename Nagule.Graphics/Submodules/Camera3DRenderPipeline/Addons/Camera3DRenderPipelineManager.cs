namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public class Camera3DRenderPipelineManager : ViewBase<TypeUnion<Camera3D>>
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef e, in Camera3D.SetPriority cmd) => {
            var pipeline = e.FindReferred<RenderPipeline>();
            if (pipeline.HasValue) {
                pipeline.Value.RenderPipeline_SetPriority(cmd.Value);
            }
        });
    }

    protected override void OnEntityAdded(in EntityRef camera)
    {
        var cameraCopy = camera;
        if (!camera.Contains<Feature>()) {
            return;
        }
        World.GetAddon<SimulationFramer>().Start(() => {
            RenderPipeline.CreateEntity(World, new() {
                Camera = cameraCopy,
                Passes = RenderPipelineUtils.ConstructPasses(cameraCopy.GetFeatureNode()),
                Priority = cameraCopy.Get<Camera3D>().Priority
            }, cameraCopy);
        });
    }

    protected override void OnEntityRemoved(in EntityRef entity) {}
}