namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using PixelFormat = Nagule.Graphics.PixelFormat;
using GLPixelFormat = global::OpenTK.Graphics.OpenGL.PixelFormat;

internal unsafe static class GLHelper
{
    public const int BuiltInBufferCount = 5;

    private static readonly float[] s_transparencyClearColor = {0, 0, 0, 1};

    public static IntPtr InitializeBuffer(BufferTargetARB target, int length)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            GL.BufferData(target, length, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        }
        else {
            GL.BufferStorage((BufferStorageTarget)target, length, IntPtr.Zero,
                BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
        }
        return (IntPtr)GL.MapBuffer(target, BufferAccessARB.WriteOnly);
    }

    public static void EnableMatrix4x4Attributes(uint startIndex, uint divisor = 0)
    {
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 0);
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 2 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 3 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);
    }

    public static void TexImage2D(TextureType type, ImageBase image)
        => TexImage2D(TextureTarget.Texture2d, type, image);

    public static void TexImage2D(TextureTarget target, TextureType type, ImageBase image)
    {
        var pixelFormat = image.PixelFormat;
        int width = image.Width;
        int height = image.Height;

        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            switch (image) {
            case Image byteImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R8,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16f,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32f,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreen:
            switch (image) {
            case Image byteImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg8,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16f,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32f,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlue:
            switch (image) {
            case Image byteImage:
                format = type switch {
                    TextureType.Color => InternalFormat.Srgb8,
                    TextureType.UI => InternalFormat.Srgb8,
                    _ => InternalFormat.Rgb8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16f,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32f,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlueAlpha:
            switch (image) {
            case Image byteImage:
                format = type switch {
                    TextureType.Color => InternalFormat.Srgb8Alpha8,
                    TextureType.UI => InternalFormat.Srgb8Alpha8,
                    _ => InternalFormat.Rgba8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16f,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32f,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        }
        throw new NotSupportedException("Pixel format not supported: " + pixelFormat);
    }

    public static void TexImage2D(TextureType type, PixelFormat pixelFormat, int width, int height)
    {
        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.R8,
                width, height, 0, GLPixelFormat.Red,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreen:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.Rg8,
                width, height, 0, GLPixelFormat.Rg,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlue:
            format = type switch {
                TextureType.Color => InternalFormat.Srgb8,
                TextureType.UI => InternalFormat.Srgb8,
                _ => InternalFormat.Rgb8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgb,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = type switch {
                TextureType.Color => InternalFormat.Srgb8Alpha8,
                TextureType.UI => InternalFormat.Srgb8Alpha8,
                _ => InternalFormat.Rgba8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgba,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        }
    }
    
    public static void WaitSync(GLSync sync)
    {
        SyncStatus status;
        do {
            status = GL.ClientWaitSync(sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);
    }

    public static void FenceSync(ref GLSync sync)
    {
        GL.DeleteSync(sync);
        sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        GL.Flush();
    }

    public static void Clear(ClearFlags flags)
    {
        switch (flags) {
        case ClearFlags.Color | ClearFlags.Depth:
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            break;
        case ClearFlags.Color:
            GL.Clear(ClearBufferMask.ColorBufferBit);
            break;
        case ClearFlags.Depth:
            GL.Clear(ClearBufferMask.DepthBufferBit);
            break;
        }
    }

    public static void GenerateHiZBuffer(ICommandHost host, in RenderPipelineData pipeline)
    {
        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        ref var hizProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.HierarchicalZShaderProgramId);
        if (Unsafe.IsNullRef(ref hizProgram)) { return; }
        GL.UseProgram(hizProgram.Handle);

        // downsample depth buffer to hi-Z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.DepthTextureHandle);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.HiZTextureHandle, 0);
        
        GL.Viewport(0, 0, pipeline.HiZWidth, pipeline.HiZHeight);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        // generate hi-z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.HiZTextureHandle);

        int width = pipeline.HiZWidth;
        int height = pipeline.HiZHeight;
        int levelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        for (int i = 1; i < levelCount; ++i) {
            width /= 2;
            height /= 2;
            width = width > 0 ? width : 1;
            height = height > 0 ? height : 1;
            GL.Viewport(0, 0, width, height);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, i - 1);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, i - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.HiZTextureHandle, i);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, pipeline.Width, pipeline.Height);
    }

    public static void CullMeshes(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds, bool excludeOccluders = true)
    {
        ref var cullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.CullingShaderProgramId);
        if (Unsafe.IsNullRef(ref cullProgram)) {
            return;
        }

        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.HiZTextureHandle);

        if (excludeOccluders) {
            foreach (var id in meshIds) {
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                if (meshData.IsOccluder) { continue; }
                Cull(host, id, in meshData);
            }
        }
        else {
            foreach (var id in meshIds) {
                ref readonly var meshData = ref host.Inspect<MeshData>(id);
                Cull(host, id, in meshData);
            }
        }

        GL.Disable(EnableCap.RasterizerDiscard);
    }

    public static void CullOccluderMeshes(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds)
    {
        ref var occluderCullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.OccluderCullingShaderProgramId);
        if (Unsafe.IsNullRef(ref occluderCullProgram)) {
            return;
        }

        GL.UseProgram(occluderCullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            Cull(host, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);
    }

    private static void Cull(ICommandHost host, Guid id, in MeshData meshData)
    {
        ref var state = ref host.RequireOrNullRef<MeshRenderState>(id);
        if (Unsafe.IsNullRef(ref state)) { return; }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshData.UniformBufferHandle);
        GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, meshData.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BindVertexArray(meshData.CullingVertexArrayHandle);

        GL.BeginTransformFeedback(PrimitiveType.Points);
        GL.BeginQuery(QueryTarget.PrimitivesGenerated, meshData.CulledQueryHandle);
        GL.DrawArrays(PrimitiveType.Points, 0, state.InstanceCount);
        GL.EndQuery(QueryTarget.PrimitivesGenerated);
        GL.EndTransformFeedback();
    }

    public static void ActivateBuiltInTextures(ICommandHost host, in RenderPipelineData pipeline)
    {
        ref var defaultTexData = ref host.RequireOrNullRef<TextureData>(Graphics.DefaultTextureId);
        if (Unsafe.IsNullRef(ref defaultTexData)) {
            return;
        }

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.DepthTextureHandle);

        var lightBufferHandle = host.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref host.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);
    }

    public static void RenderOpaque(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds)
    {
        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            Draw(host, in pipeline, id, in meshData);
        }
    }

    public static void RenderDepth(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds)
    {
        GL.ColorMask(false, false, false, false);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            DrawDepth(host, in pipeline, id, in meshData);
        }

        GL.ColorMask(true, true, true, true);
    }

    public static void RenderBlending(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds)
    {
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            if (meshData.RenderMode == RenderMode.Additive) {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            }
            else {
                // meshData.RenderMode == RenderMode.Multiplicative
                GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
            }
            Draw(host, in pipeline, id, in meshData);
        }

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
    }

    public static void RenderTransparent(ICommandHost host, in RenderPipelineData pipeline, ReadOnlySpan<Guid> meshIds)
    {
        ref var composeProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.TransparencyComposeShaderProgramId);
        if (Unsafe.IsNullRef(ref composeProgram)) {
            return;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipeline.TransparencyFramebufferHandle);
        GL.ClearBufferf(Buffer.Color, 0, s_transparencyClearColor);
        GL.ClearBufferf(Buffer.Color, 1, s_transparencyClearColor);

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            Draw(host, in pipeline, id, in meshData);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipeline.ColorFramebufferHandle);

        // compose transparency

        GL.UseProgram(composeProgram.Handle);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.TransparencyAccumTextureHandle);
        GL.Uniform1i(composeProgram.TextureLocations!["AccumTex"], GLHelper.BuiltInBufferCount);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount + 1);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.TransparencyRevealTextureHandle);
        GL.Uniform1i(composeProgram.TextureLocations["RevealTex"], GLHelper.BuiltInBufferCount + 1);

        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    public static void Draw(ICommandHost host, in RenderPipelineData pipeline, Guid meshId, in MeshData meshData)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount == 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref host.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref host.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyMaterial(host, in pipeline, matId, in materialData);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    public static void DrawDepth(ICommandHost host, in RenderPipelineData pipeline, Guid meshId, in MeshData meshData)
    {
        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);
        if (visibleCount > 0) { return; }

        var matId = meshData.MaterialId;
        ref var materialData = ref host.RequireOrNullRef<MaterialData>(matId);

        if (Unsafe.IsNullRef(ref materialData)) {
            materialData = ref host.RequireOrNullRef<MaterialData>(Graphics.DefaultMaterialId);
            if (Unsafe.IsNullRef(ref materialData)) { return; }
        }

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        ApplyDepthMaterial(host, in pipeline, matId, in materialData);
        GL.DrawElementsInstanced(meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    public static void DrawCubemapSkybox(ICommandHost host, TextureHandle textureHandle)
    {
        ref var skyboxProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.SkyboxShaderProgramId);
        if (Unsafe.IsNullRef(ref skyboxProgram)) {
            return;
        }

        GL.UseProgram(skyboxProgram.Handle);
        GL.DepthMask(false);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.TextureCubeMap, textureHandle);
        GL.Uniform1i(skyboxProgram.TextureLocations!["SkyboxTex"], GLHelper.BuiltInBufferCount);

        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.DepthMask(true);
    }

    public static void ApplyMaterial(ICommandHost host, in RenderPipelineData pipeline, Guid id, in MaterialData materialData)
    {
        ref var programData = ref host.RequireOrNullRef<GLSLProgramData>(materialData.ShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(host, in programData, in materialData);
    }

    public static void ApplyDepthMaterial(ICommandHost host, in RenderPipelineData pipeline, Guid id, in MaterialData materialData)
    {
        ref var programData = ref host.RequireOrNullRef<GLSLProgramData>(materialData.DepthShaderProgramId);
        if (Unsafe.IsNullRef(ref programData)) {
            programData = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.DefaultDepthShaderProgramId);
            if (Unsafe.IsNullRef(ref programData)) { return; }
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(programData.Handle);

        EnableBuiltInBuffers(in programData);
        EnableTextures(host, in programData, in materialData);
    }

    public static void EnableBuiltInBuffers(in GLSLProgramData programData)
    {
        if (programData.DepthBufferLocation != -1) {
            GL.Uniform1i(programData.DepthBufferLocation, 1);
        }
        if (programData.LightsBufferLocation != -1) {
            GL.Uniform1i(programData.LightsBufferLocation, 2);
        }
        if (programData.ClustersBufferLocation != -1) {
            GL.Uniform1i(programData.ClustersBufferLocation, 3);
        }
        if (programData.ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1i(programData.ClusterLightCountsBufferLocation, 4);
        }
    }

    public static void EnableTextures(ICommandHost host, in GLSLProgramData programData, in MaterialData materialData)
    {
        var textures = materialData.Textures;
        var textureLocations = programData.TextureLocations;

        if (textures == null || textureLocations == null) {
            return;
        }

        uint texUnitIndex = BuiltInBufferCount;

        foreach (var (name, texId) in textures) {
            if (!textureLocations.TryGetValue(name, out var location)) {
                continue;
            }

            ref var texData = ref host.RequireOrNullRef<TextureData>(texId);
            if (Unsafe.IsNullRef(ref texData)) {
                GL.Uniform1i(location, 0);
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + texUnitIndex);
            GL.BindTexture(TextureTarget.Texture2d, texData.Handle);
            GL.Uniform1i(location, (int)texUnitIndex);
            
            ++texUnitIndex;
        }
    }
}