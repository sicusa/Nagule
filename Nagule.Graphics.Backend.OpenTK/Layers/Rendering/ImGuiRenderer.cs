// from https://github.com/NogginBops/ImGui.NET_OpenTK_Sample/blob/opentk5/Dear%20ImGui%20Sample/ImGuiController.cs

namespace Nagule.Graphics.Backend.OpenTK;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Mathematics;
using global::OpenTK.Graphics;

using ImGuiNET;

using Aeco;

using ErrorCode = global::OpenTK.Graphics.OpenGL.ErrorCode;
using PixelFormat = global::OpenTK.Graphics.OpenGL.PixelFormat;

public class ImGuiRenderer : VirtualLayer,
    ILoadListener, IWindowResizeListener, IFrameStartListener, IRenderListener
{
    private VertexArrayHandle _vertexArray;
    private BufferHandle _vertexBuffer;
    private int _vertexBufferSize;
    private BufferHandle _indexBuffer;
    private int _indexBufferSize;

    private TextureHandle _fontTexture;

    private ProgramHandle _shader;
    private int _shaderFontTextureLocation;
    private int _shaderProjectionMatrixLocation;
    
    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor;

    private static bool KHRDebugAvailable = false;

    public unsafe void OnLoad(IContext context)
    {
        ref readonly var screen = ref context.InspectAny<Screen>();
        _windowWidth = screen.Width;
        _windowHeight = screen.Height;
        _scaleFactor = new System.Numerics.Vector2(screen.WidthScale, screen.HeightScale);

        int major = 0;  GL.GetInteger(GetPName.MajorVersion, ref major);
        int minor = 0;  GL.GetInteger(GetPName.MinorVersion, ref minor);

        KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

        IntPtr imGuiCtx = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiCtx);

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset
            | ImGuiBackendFlags.HasMouseCursors;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
            | ImGuiConfigFlags.NavEnableGamepad
            | ImGuiConfigFlags.DockingEnable
            | ImGuiConfigFlags.IsSRGB;
        io.ConfigWindowsResizeFromEdges = true;
        io.FontGlobalScale = 1 / _scaleFactor.X;

        ImGuiHelper.SetDefaultStyle();

        var font = InternalAssets.Load<Font>("Nagule.Graphics.Backend.OpenTK.Embeded.Fonts.DroidSans.ttf");
        ImGuiHelper.AddFont(context, font, 15);

        CreateDeviceResources();
        ImGuiHelper.SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
    }

/*
    public void OnUnload(IContext context)
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
    }*/

    public void OnWindowResize(IContext context, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight) / _scaleFactor;
    }

    private void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        int prevVAO = 0;  GL.GetInteger(GetPName.VertexArrayBinding, ref prevVAO);
        int prevArrayBuffer = 0;  GL.GetInteger(GetPName.ArrayBufferBinding, ref prevArrayBuffer);

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);
        LabelObject(ObjectIdentifier.VertexArray, (int)_vertexArray, "ImGui");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        LabelObject(ObjectIdentifier.Buffer, (int)_vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTargetARB.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        LabelObject(ObjectIdentifier.Buffer, (int)_indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTargetARB.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);

        RecreateFontDeviceTexture();

        string VertexSource = @"#version 330 core
uniform mat4 projection_matrix;
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;
out vec4 color;
out vec2 texCoord;
void main()
{
gl_Position = projection_matrix * vec4(in_position, 0, 1);
color = in_color;
texCoord = in_texCoord;
}";
        string FragmentSource = @"#version 330 core
uniform sampler2D in_fontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
outputColor = color * texture(in_fontTexture, texCoord);
}";

        _shader = CreateProgram("ImGui", VertexSource, FragmentSource);
        _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
        _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray((VertexArrayHandle)prevVAO);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, (BufferHandle)prevArrayBuffer);

        CheckGLError("End of ImGui setup");
    }

    private void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        int prevActiveTexture = 0;
        int prevTexture2D = 0;

        GL.GetInteger(GetPName.ActiveTexture, ref prevActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.GetInteger(GetPName.TextureBinding2d, ref prevTexture2D);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, _fontTexture);
        GL.TexStorage2D(TextureTarget.Texture2d, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectIdentifier.Texture, (int)_fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

        GL.GenerateMipmap(TextureTarget.Texture2d);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, mips - 1);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2d, (TextureHandle)prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID((IntPtr)(int)_fontTexture);
        io.Fonts.ClearTexData();
    }

    public void OnFrameStart(IContext context, float deltaTime)
    {
        SetPerFrameImGuiData(deltaTime);
        ImGui.NewFrame();
    }

    public void OnRender(IContext context, float deltaTime)
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private void SetPerFrameImGuiData(float deltaTime)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaTime;
    }

    private void RenderImDrawData(ImDrawDataPtr draw_data)
    {
        if (draw_data.CmdListsCount == 0) {
            return;
        }

        // Get intial state.
        int prevVAO = 0; GL.GetInteger(GetPName.VertexArrayBinding, ref prevVAO);
        int prevArrayBuffer = 0;  GL.GetInteger(GetPName.ArrayBufferBinding, ref prevArrayBuffer);
        int prevProgram = 0;  GL.GetInteger(GetPName.CurrentProgram, ref prevProgram);
        bool prevBlendEnabled = false;  GL.GetBoolean(GetPName.Blend, ref prevBlendEnabled);
        bool prevScissorTestEnabled = false;  GL.GetBoolean(GetPName.ScissorTest, ref prevScissorTestEnabled);
        int prevBlendEquationRgb = 0;  GL.GetInteger(GetPName.BlendEquationRgb, ref prevBlendEquationRgb);
        int prevBlendEquationAlpha = 0;  GL.GetInteger(GetPName.BlendEquationAlpha, ref prevBlendEquationAlpha);
        int prevBlendFuncSrcRgb = 0;  GL.GetInteger(GetPName.BlendSrcRgb, ref prevBlendFuncSrcRgb);
        int prevBlendFuncSrcAlpha = 0;  GL.GetInteger(GetPName.BlendSrcAlpha, ref prevBlendFuncSrcAlpha);
        int prevBlendFuncDstRgb = 0;  GL.GetInteger(GetPName.BlendDstRgb, ref prevBlendFuncDstRgb);
        int prevBlendFuncDstAlpha = 0;  GL.GetInteger(GetPName.BlendDstAlpha, ref prevBlendFuncDstAlpha);
        bool prevCullFaceEnabled = false;  GL.GetBoolean(GetPName.CullFace, ref prevCullFaceEnabled);
        bool prevDepthTestEnabled = false;  GL.GetBoolean(GetPName.DepthTest, ref prevDepthTestEnabled);
        int prevActiveTexture = 0;  GL.GetInteger(GetPName.ActiveTexture, ref prevActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = 0;  GL.GetInteger(GetPName.TextureBinding2d, ref prevTexture2D);
        Span<int> prevScissorBox = stackalloc int[4];
        GL.GetInteger(GetPName.ScissorBox, prevScissorBox);

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        
        for (int i = 0; i < draw_data.CmdListsCount; i++) {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize) {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                
                GL.BufferData(BufferTargetARB.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                _vertexBufferSize = newSize;

                Console.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
            }

            int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize) {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                _indexBufferSize = newSize;

                Console.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
            }
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            _windowWidth,
            _windowHeight,
            0.0f,
            -1.0f,
            1.0f);

        GL.UseProgram(_shader);
        GL.UniformMatrix4f(_shaderProjectionMatrixLocation, false, in mvp);
        GL.Uniform1i(_shaderFontTextureLocation, 0);
        CheckGLError("Projection");

        GL.BindVertexArray(_vertexArray);
        CheckGLError("VAO");

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationModeEXT.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        GL.Clear(ClearBufferMask.StencilBufferBit);

        // Render command lists
        for (int n = 0; n < draw_data.CmdListsCount; n++) {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];

            for (int i = 0; i < cmd_list.VtxBuffer.Size; i++) {
                var v = cmd_list.VtxBuffer[i];
                v.pos.X *= _scaleFactor.X;
                v.pos.Y *= _scaleFactor.Y;
            }

            GL.BufferSubData(BufferTargetARB.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
            CheckGLError($"Data Vert {n}");

            GL.BufferSubData(BufferTargetARB.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
            CheckGLError($"Data Idx {n}");

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++) {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero) {
                    throw new NotImplementedException();
                }
                else {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2d, (TextureHandle)(int)pcmd.TextureId);
                    CheckGLError("Texture");

                    // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                    var clip = pcmd.ClipRect;
                    GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                    CheckGLError("Scissor");

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0) {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                    }
                    else {
                        GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }
                    CheckGLError("Draw");
                }
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        // Reset state
        GL.BindTexture(TextureTarget.Texture2d, (TextureHandle)prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.UseProgram((ProgramHandle)prevProgram);
        GL.BindVertexArray((VertexArrayHandle)prevVAO);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, (BufferHandle)prevArrayBuffer);
        GL.BlendEquationSeparate((BlendEquationModeEXT)prevBlendEquationRgb, (BlendEquationModeEXT)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactor)prevBlendFuncSrcRgb,
            (BlendingFactor)prevBlendFuncDstRgb,
            (BlendingFactor)prevBlendFuncSrcAlpha,
            (BlendingFactor)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
    }

    private static void LabelObject(ObjectIdentifier objLabelIdent, int glObject, string name)
    {
        if (KHRDebugAvailable) {
            GL.ObjectLabel(objLabelIdent, (uint)glObject, name.Length, name);
        }
    }

    private static bool IsExtensionSupported(string name)
    {
        int n = 0;  GL.GetInteger(GetPName.NumExtensions, ref n);
        for (int i = 0; i < n; i++) {
            string? extension = GL.GetStringi(StringName.Extensions, (uint)i);
            if (extension == name) return true;
        }

        return false;
    }

    public static ProgramHandle CreateProgram(string name, string vertexSource, string fragmentSoruce)
    {
        ProgramHandle program = GL.CreateProgram();
        LabelObject(ObjectIdentifier.Program, (int)program, $"Program: {name}");

        ShaderHandle vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        ShaderHandle fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        int success = 0;
        GL.GetProgrami(program, ProgramPropertyARB.LinkStatus, ref success);

        if (success == 0) {
            GL.GetProgramInfoLog(program, out string info);
            Debug.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static ShaderHandle CompileShader(string name, ShaderType type, string source)
    {
        ShaderHandle shader = GL.CreateShader(type);
        LabelObject(ObjectIdentifier.Shader, (int)shader, $"Shader: {name}");

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        int success = 0;
        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, ref success);

        if (success == 0) {
            GL.GetShaderInfoLog(shader, out string info);
            Debug.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }

    public static void CheckGLError(string title)
    {
        ErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError) {
            Debug.Print($"{title} ({i++}): {error}");
        }
    }
}