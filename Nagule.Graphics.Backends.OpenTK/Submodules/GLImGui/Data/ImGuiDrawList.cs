namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;
using ImGuiNET;

public class ImGuiDrawList : IDisposable
{
    [AllowNull] public MemoryOwner<ImDrawVert> VtxBuffer { get; private set; }
    [AllowNull] public MemoryOwner<ushort> IdxBuffer { get; private set; }
    [AllowNull] public MemoryOwner<ImDrawCmd> CmdBuffer { get; private set; }

    private static readonly ConcurrentStack<ImGuiDrawList> s_pool = new();

    private ImGuiDrawList() {}

    public static ImGuiDrawList Create(int vtxBufferSize, int idxBufferSize, int cmdBufferSize)
    {
        var res = s_pool.TryPop(out var list) ? list : new();
        res.VtxBuffer = MemoryOwner<ImDrawVert>.Allocate(vtxBufferSize);
        res.IdxBuffer = MemoryOwner<ushort>.Allocate(idxBufferSize);
        res.CmdBuffer = MemoryOwner<ImDrawCmd>.Allocate(cmdBufferSize);
        return res;
    }
    
    public void Dispose()
    {
        VtxBuffer.Dispose();
        VtxBuffer = null;

        IdxBuffer.Dispose();
        IdxBuffer = null;

        CmdBuffer.Dispose();
        CmdBuffer = null;

        s_pool.Push(this);
    }
}