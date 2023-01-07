namespace Nagule.Graphics;

public static class Graphics
{
    public static Guid RootId { get; } = Guid.Parse("58808b2a-9c92-487e-aef8-2b60ea766cad");
    public static Guid DefaultTextureId { get; } = Guid.Parse("9a621b14-5b03-4b12-a3ac-6f317a5ed431");
    public static Guid DefaultMaterialId { get; } = Guid.Parse("9a621b14-5b03-4b12-a3ac-6f317a5ed432");

    public static Guid DefaultOpaqueShaderProgramId { get; } = Guid.Parse("fa55827a-852c-4de2-b47e-3df941ec7619");
    public static Guid DefaultTransparentShaderProgramId { get; } = Guid.Parse("f03617c0-61c3-415c-bc37-ffce0e652de3");
    public static Guid DefaultCutoffShaderProgramId { get; } = Guid.Parse("f03617c0-61c3-415c-bc37-ffce0e652de4");

    public static Guid DefaultUnlitShaderProgramId { get; } = Guid.Parse("fa55827a-852c-4de2-b47f-3df941ec7619");
    public static Guid DefaultUnlitTransparentShaderProgramId { get; } = Guid.Parse("f03617c0-61c3-415c-bc38-ffce0e652de3");
    public static Guid DefaultUnlitCutoffShaderProgramId { get; } = Guid.Parse("f03617c0-61c3-415c-bc38-ffce0e652de4");

    public static Guid DefaultDepthShaderProgramId { get; } = Guid.Parse("fa55827a-852c-4de2-b47e-3df941ec7620");

    public static Guid CullingShaderProgramId { get; } = Guid.Parse("ff7d8e33-eeb5-402b-b633-e2b2a264b1e9");
    public static Guid OccluderCullingShaderProgramId { get; } = Guid.Parse("ff7d8e33-eeb5-402b-b633-e2b2a264b1f0");
    public static Guid HierarchicalZShaderProgramId { get; } = Guid.Parse("b04b536e-3e4a-4896-b289-6f8910746ef2");
    public static Guid TransparencyComposeShaderProgramId { get; } = Guid.Parse("e7c34862-7de2-494f-b7ae-272659d1a752");
    public static Guid PostProcessingShaderProgramId { get; } = Guid.Parse("8fa594b9-3c16-4996-b7e1-c9cb36037aa2");
    public static Guid DebugPostProcessingShaderProgramId { get; } = Guid.Parse("8fa594b9-3c16-4996-b7e1-c9cb36037aa3");
}