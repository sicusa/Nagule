namespace Nagule.Graphics;

using Sia;

public class SceneCamera3DManager : ViewBase<TypeUnion<Camera3D>>
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef e, in Camera3D.SetPriority cmd) => {
            if (!e.Contains<Feature>()) {
                return;
            }

            var renderer = world.GetAddon<SceneCamera3DRenderer>();
            var pipelineStateEntity = e.FindReferred<RenderPipeline>()!.Value.GetStateEntity();
            var priority = cmd.Value;

            world.GetAddon<RenderFramer>().Enqueue(e, () => {
                ref var state = ref pipelineStateEntity.Get<RenderPipelineState>();
                while (!state.Loaded) {
                    return false;
                }
                var scheduler = state.Scheduler;

                var entries = renderer.Entries;
                int prevIndex = entries.FindIndex(e => e.Scheduler == scheduler);
                entries.RemoveAt(prevIndex);

                var newIndex = entries.FindIndex(e => e.Priority >= priority);
                if (newIndex == -1) {
                    newIndex = entries.Count;
                }
                entries.Insert(newIndex, new(priority, scheduler));
                return true;
            });
        });
    }

    protected override void OnEntityAdded(in EntityRef camera)
    {
        if (!camera.Contains<Feature>()) {
            return;
        }
        var cameraCopy = camera;
        World.GetAddon<SimulationFramer>().Start(() => {
            var pipelineEntity = RenderPipeline.CreateEntity(World, new() {
                Camera = cameraCopy,
                Passes = RenderPipelineUtils.ConstructFeaturePasses(cameraCopy.GetFeatureNode())
            }, cameraCopy);
            var pipelineStateEntity = pipelineEntity.GetStateEntity();

            var renderer = World.GetAddon<SceneCamera3DRenderer>();
            var priority = cameraCopy.Get<Camera3D>().Priority;

            World.GetAddon<RenderFramer>().Start(() => {
                ref var pipelineState = ref pipelineStateEntity.Get<RenderPipelineState>();
                if (!pipelineState.Loaded) {
                    return false;
                }
                var entries = renderer.Entries;
                var index = entries.FindIndex(e => e.Priority >= priority);
                if (index == -1) {
                    index = entries.Count;
                }
                entries.Insert(index, new(priority, pipelineState.Scheduler));
                return true;
            });
        });
    }

    protected override void OnEntityRemoved(in EntityRef entity) {}
}