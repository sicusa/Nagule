namespace Nagule.Graphics;

public static class Graphics
{
    public static Guid RootId { get; } = Guid.Parse("58808b2a-9c92-487e-aef8-2b60ea766cad");
    public static Guid DefaultMaterialId { get; } = Guid.Parse("9a621b14-5b03-4b12-a3ac-6f317a5ed432");

    public static Guid DefaultShaderProgramId { get; } = Guid.Parse("fa55827a-852c-4de2-b47e-3df941ec7619");
    public static Guid DefaultDepthShaderProgramId { get; } = Guid.Parse("fa55827a-852c-4de2-b47e-3df941ec7620");

    public static Guid PostProcessingShaderProgramId { get; } = Guid.Parse("8fa594b9-3c16-4996-b7e1-c9cb36037aa2");
    public static Guid DebugPostProcessingShaderProgramId { get; } = Guid.Parse("8fa594b9-3c16-4996-b7e1-c9cb36037aa3");
}