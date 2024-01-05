namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using CommunityToolkit.HighPerformance.Buffers;

public struct ImGuiLayerState : IAssetState
{
    public readonly bool Loaded => VertexArray != VertexArrayHandle.Zero;
    
    public MemoryOwner<ImGuiDrawList>? DrawLists;

    public VertexArrayHandle VertexArray;
    public BufferHandle VertexBuffer;
    public int VertexBufferSize;
    public BufferHandle IndexBuffer;
    public int IndexBufferSize;
    public TextureHandle FontTexture;

    public ProgramHandle ShaderProgram;
    public int ShaderFontTextureLocation;
    public int ShaderProjectionMatrixLocation;
}