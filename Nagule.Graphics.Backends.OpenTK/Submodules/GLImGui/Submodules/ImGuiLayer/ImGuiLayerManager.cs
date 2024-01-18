namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Nagule.Graphics.UI;
using Sia;

public class ImGuiLayerManager
    : GraphicsAssetManager<ImGuiLayer, RImGuiLayer,
        Tuple<ImGuiLayerState, ImGuiContext, RenderPipelineProvider>>
{
    [AllowNull] private ImGuiEventDispatcher _dispatcher;

    private static bool KHRDebugAvailable = false;
    private static readonly string s_vertexSource =
        EmbeddedAssets.LoadInternal<RText>("shaders.imgui.vert.glsl");
    private static readonly string s_fragmentSource =
        EmbeddedAssets.LoadInternal<RText>("shaders.imgui.frag.glsl");

    private class DrawImGuiPassProvider(EntityRef layerEntity) : IRenderPipelineProvider
    {
        public SystemChain TransformPipeline(in EntityRef entity, SystemChain chain)
            => chain.Add<DrawImGuiPass>(() => new(layerEntity));
    }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _dispatcher = world.GetAddon<ImGuiEventDispatcher>();

        RenderFramer.Start(() => {
            int major = 0; GL.GetInteger(GetPName.MajorVersion, ref major);
            int minor = 0; GL.GetInteger(GetPName.MinorVersion, ref minor);

            KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");
            return true;
        });
    }

    protected override void LoadAsset(EntityRef entity, ref ImGuiLayer asset, EntityRef stateEntity)
    {
        stateEntity.Get<RenderPipelineProvider>().Instance = new DrawImGuiPassProvider(entity);

        IntPtr imGuiCtx = ImGui.CreateContext();
        //ImGui.SetCurrentContext(imGuiCtx);
        stateEntity.Get<ImGuiContext>().Pointer = imGuiCtx;

        InitializeImGui();
        ImGui.NewFrame();

        RenderFramer.Enqueue(entity, () => {
            CreateDeviceResources(ref stateEntity.Get<ImGuiLayerState>());
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref ImGuiLayer asset, EntityRef stateEntity)
    {
        var imGuiCtx = stateEntity.Get<ImGuiContext>().Pointer;
        ImGui.DestroyContext(imGuiCtx);

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<ImGuiLayerState>();
            GL.DeleteVertexArray(state.VertexArray.Handle);
            GL.DeleteBuffer(state.VertexBuffer.Handle);
            GL.DeleteBuffer(state.IndexBuffer.Handle);
            GL.DeleteTexture(state.FontTexture.Handle);
            GL.DeleteProgram(state.ShaderProgram.Handle);
        });
    }

    private void InitializeImGui()
    {
        var io = ImGui.GetIO();
        float screenScale = _dispatcher.ScreenScale.X;

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset
            | ImGuiBackendFlags.HasMouseCursors;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard
            | ImGuiConfigFlags.NavEnableGamepad
            | ImGuiConfigFlags.DockingEnable
            | ImGuiConfigFlags.IsSRGB;

        io.ConfigWindowsResizeFromEdges = true;
        io.FontGlobalScale = 1 / screenScale;

        //ImGuiUtils.SetDefaultStyle();

        var font = EmbeddedAssets.LoadInternal<RFont>(
            "Fonts.DroidSans.ttf", typeof(RFont).Assembly);
        ImGuiUtils.AddFont(font, 15, screenScale);

        io.DisplaySize = new Vector2(
            1024,
            768);
        io.DisplayFramebufferScale = _dispatcher.ScreenScale;
        io.DeltaTime = 1 / 60f;
    }

    private void CreateDeviceResources(ref ImGuiLayerState state)
    {
        state.VertexBufferSize = 10000;
        state.IndexBufferSize = 2000;

        int prevVAO = 0;  GL.GetInteger(GetPName.VertexArrayBinding, ref prevVAO);
        int prevArrayBuffer = 0;  GL.GetInteger(GetPName.ArrayBufferBinding, ref prevArrayBuffer);

        int vertexArray = GL.GenVertexArray();
        state.VertexArray = new(vertexArray);
        GL.BindVertexArray(vertexArray);
        LabelObject(ObjectIdentifier.VertexArray, vertexArray, "ImGui");

        int vertexBuffer = GL.GenBuffer();
        state.VertexBuffer = new(vertexBuffer);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
        LabelObject(ObjectIdentifier.Buffer, vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTargetARB.ArrayBuffer, state.VertexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);

        int indexBuffer = GL.GenBuffer();
        state.IndexBuffer = new(indexBuffer);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);
        LabelObject(ObjectIdentifier.Buffer, indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTargetARB.ElementArrayBuffer, state.IndexBufferSize, IntPtr.Zero, BufferUsageARB.DynamicDraw);

        RecreateFontDeviceTexture(ref state);

        int shader = CreateProgram("ImGui", s_vertexSource, s_fragmentSource);
        state.ShaderProgram = new(shader);
        state.ShaderProjectionMatrixLocation = GL.GetUniformLocation(shader, "projection_matrix");
        state.ShaderFontTextureLocation = GL.GetUniformLocation(shader, "in_fontTexture");

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, prevArrayBuffer);

        CheckGLError("End of ImGui setup");
    }

    private static void RecreateFontDeviceTexture(ref ImGuiLayerState state)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        int prevActiveTexture = 0;
        int prevTexture2D = 0;

        GL.GetInteger(GetPName.ActiveTexture, ref prevActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.GetInteger(GetPName.TextureBinding2d, ref prevTexture2D);

        int fontTexture = GL.GenTexture();
        state.FontTexture = new(fontTexture);
        GL.BindTexture(TextureTarget.Texture2d, fontTexture);
        GL.TexStorage2D(TextureTarget.Texture2d, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectIdentifier.Texture, fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2d, 0, 0, 0, width, height, GLPixelFormat.Bgra, PixelType.UnsignedByte, pixels);
        GL.GenerateMipmap(TextureTarget.Texture2d);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.Repeat);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.Repeat);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, mips - 1);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2d, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID(fontTexture);
        io.Fonts.ClearTexData();
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

    public int CreateProgram(string name, string vertexSource, string fragmentSoruce)
    {
        int program = GL.CreateProgram();
        LabelObject(ObjectIdentifier.Program, program, $"Program: {name}");

        var vertex = CompileShader(name, GLShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(name, GLShaderType.FragmentShader, fragmentSoruce);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        int success = 0;
        GL.GetProgrami(program, ProgramPropertyARB.LinkStatus, ref success);

        if (success == 0) {
            GL.GetProgramInfoLog(program, out string info);
            Logger.LogInformation("GL.LinkProgram had info log [{Name}]:\n{Info}", name, info);
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private int CompileShader(string name, GLShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        LabelObject(ObjectIdentifier.Shader, shader, $"Shader: {name}");

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        int success = 0;
        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, ref success);

        if (success == 0) {
            GL.GetShaderInfoLog(shader, out string info);
            Logger.LogInformation("GL.CompileShader for shader '{Name}' [{Type}] had info log:\n{Info}", name, type, info);
        }

        return shader;
    }

    public void CheckGLError(string title)
    {
        GLErrorCode error;
        int i = 1;
        while ((error = GL.GetError()) != GLErrorCode.NoError) {
            Logger.LogError("{title} ({Count}): {Error}", title, i++, error);
        }
    }
}