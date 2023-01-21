namespace Nagule.Graphics.Backend.OpenTK;

public class RenderSettingsManager : ResourceManagerBase<RenderSettings>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;
        public Guid? SkyboxId;

        public override Guid? Id => RenderSettingsId;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<RenderSettingsData>(RenderSettingsId);
            data.SkyboxId = SkyboxId;
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;

        public override void Execute(ICommandContext context)
        {
            context.Remove<RenderSettingsData>(RenderSettingsId);
        }
    }

    protected override void Initialize(IContext context, Guid id, RenderSettings resource, RenderSettings? prevResource)
    {
        if (prevResource != null) {
            UnreferenceDependencies(context, id);
        }

        var cmd = InitializeCommand.Create();
        cmd.RenderSettingsId = id;

        if (resource.Skybox != null) {
            cmd.SkyboxId = ResourceLibrary<Cubemap>.Reference(context, id, resource.Skybox);
        }

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, RenderSettings resource)
    {
        UnreferenceDependencies(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderSettingsId = id;
        context.SendCommandBatched(cmd);
    }

    private void UnreferenceDependencies(IContext context, Guid id)
    {
        ResourceLibrary<Cubemap>.UnreferenceAll(context, id);
    }
}