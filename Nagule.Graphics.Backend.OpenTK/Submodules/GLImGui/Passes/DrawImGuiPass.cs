namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nagule.Graphics.UI;
using Sia;

[AfterSystem<StageUIBeginPass>]
[BeforeSystem<StageUIFinishPass>]
public class DrawImGuiPass(EntityRef layerEntity) : RenderPassSystemBase
{
    [AllowNull] private ILogger _logger;
    [AllowNull] private ImGuiEventDispatcher _dispatcher;

    public unsafe override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _logger = world.CreateLogger<DrawImGuiPass>();
        _dispatcher = world.GetAddon<ImGuiEventDispatcher>();

        var layerState = layerEntity.GetStateEntity();

        RenderFrame.Start(() => {
            ref var state = ref layerState.Get<ImGuiLayerState>();
            RenderImDrawData(ref state);
            return NextFrame;
        });
    }

    private void RenderImDrawData(ref ImGuiLayerState state)
    {
        var drawLists = Interlocked.Exchange(ref state.DrawLists, null);
        if (drawLists == null) {
            return;
        }

        int prevVAO = 0; GL.GetInteger(GetPName.VertexArrayBinding, ref prevVAO);
        int prevArrayBuffer = 0; GL.GetInteger(GetPName.ArrayBufferBinding, ref prevArrayBuffer);

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(state.VertexArray.Handle);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, state.VertexBuffer.Handle);
        
        foreach (ref var drawList in drawLists.Span) {
            int vertexSize = drawList.VtxBuffer.Length * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > state.VertexBufferSize) {
                int newSize = (int)Math.Max(state.VertexBufferSize * 1.5f, vertexSize);
                
                GL.BufferData(BufferTargetARB.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                state.VertexBufferSize = newSize;

                _logger.LogInformation("Resized dear imgui vertex buffer to new size {Size}", newSize);
            }

            int indexSize = drawList.IdxBuffer.Length * sizeof(ushort);
            if (indexSize > state.IndexBufferSize) {
                int newSize = (int)Math.Max(state.IndexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                state.IndexBufferSize = newSize;

                _logger.LogInformation("Resized dear imgui index buffer to new size {Size}", newSize);
            }
        }

        ImGuiIOPtr io = ImGui.GetIO();

        GL.UseProgram(state.ShaderProgram.Handle);
        GL.UniformMatrix4f(state.ShaderProjectionMatrixLocation, 1, false, _dispatcher.WindowMatrix);
        GL.Uniform1i(state.ShaderFontTextureLocation, 0);
        CheckGLError("Projection");

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationModeEXT.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Clear(ClearBufferMask.StencilBufferBit);

        // Render command lists
        int n = -1;
        int windowHeight = _dispatcher.WindowHeight;

        foreach (ref var drawList in drawLists.Span) {
            ++n;

            GL.BufferSubData<ImDrawVert>(BufferTargetARB.ArrayBuffer, 0, drawList.VtxBuffer.Span);
            CheckGLError($"Data Vert {n}");

            GL.BufferSubData<ushort>(BufferTargetARB.ElementArrayBuffer, 0, drawList.IdxBuffer.Span);
            CheckGLError($"Data Idx {n}");

            foreach (ref var cmd in drawList.CmdBuffer.Span) {
                if (cmd.UserCallback != IntPtr.Zero) {
                    throw new NotImplementedException();
                }
                else {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2d, (int)cmd.TextureId);
                    CheckGLError("Texture");

                    // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                    var clip = cmd.ClipRect;
                    GL.Scissor((int)clip.X, windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                    CheckGLError("Scissor");

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0) {
                        GL.DrawElementsBaseVertex(GLPrimitiveType.Triangles, (int)cmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(cmd.IdxOffset * sizeof(ushort)), unchecked((int)cmd.VtxOffset));
                    }
                    else {
                        GL.DrawElements(GLPrimitiveType.Triangles, (int)cmd.ElemCount, DrawElementsType.UnsignedShort, (int)cmd.IdxOffset * sizeof(ushort));
                    }
                    CheckGLError("Draw");
                }
            }
            drawList.Dispose();
        }

        drawLists.Dispose();

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, prevArrayBuffer);
    }

    public void CheckGLError(string title)
    {
        GLErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != GLErrorCode.NoError) {
            _logger.LogError("{title} ({Count}): {Error}", title, i++, error);
        }
    }
}