namespace Nagule.Backend.OpenTK.Graphics;

using System.Collections.Generic;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;

public class ForwardRenderPipeline : VirtualLayer, IUpdateListener, ILoadListener, IRenderListener, IWindowResizeListener
{
    private Group<Mesh, MeshRenderingState> _g = new();
    private List<Guid> _transparentIds = new();

    private int _windowWidth;
    private int _windowHeight;
    private int _defaultVertexArray;
    private float[] _transparencyAccumClearColor = {0, 0, 0, 1};
    private float[] _minDepthClearColor = {1};

    public void OnLoad(IContext context)
    {
        _defaultVertexArray = GL.GenVertexArray();
    }

    public void OnUpdate(IContext context, float deltaTime)
        => _g.Query(context);

    public void OnRender(IContext context, float deltaTime)
    {
        ref readonly var renderTarget = ref context.Inspect<RenderTargetData>(Graphics.DefaultRenderTargetId);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ColorFramebufferHandle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Framebuffer, renderTarget.UniformBufferHandle);
        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown);
        GL.BindTexture(TextureTarget.Texture2D, renderTarget.DepthTextureHandle);

        var lightBufferHandle = context.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 1);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref context.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);

        // generate hierarchical-Z buffer

        ref readonly var hizProgram = ref context.Inspect<ShaderProgramData>(Graphics.HierarchicalZShaderProgramId);
        int lastMipSizeLocation = hizProgram.CustomLocations["LastMipSize"];
        GL.UseProgram(hizProgram.Handle);

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, renderTarget.DepthTextureHandle);

        int width = renderTarget.Width;
        int height = renderTarget.Height;
        int levelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        for (int i = 1; i < levelCount; ++i) {
            GL.Uniform2(lastMipSizeLocation, width, height);

            width /= 2;
            height /= 2;
            width = width > 0 ? width : 1;
            height = height > 0 ? height : 1;
            GL.Viewport(0, 0, width, height);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, i - 1);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, i - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, renderTarget.DepthTextureHandle, i);
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, renderTarget.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, renderTarget.Width, renderTarget.Height);

        // cull instances by camera frustum and occlusion

        var cullProgram = context.Inspect<ShaderProgramData>(Graphics.CullingShaderProgramId);
        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        foreach (var id in _g) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            Cull(context, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);

        // render opaque meshes

        ref readonly var defaultTexData = ref context.Inspect<TextureData>(Graphics.DefaultTextureId);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, defaultTexData.Handle);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        foreach (var id in _g) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            if (meshData.IsTransparent) {
                _transparentIds.Add(id);
                continue;
            }
            Render(context, id, in meshData, in renderTarget);
        }

        // render transparent objects

        if (_transparentIds.Count != 0) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.TransparencyFramebufferHandle);
            GL.ClearBuffer(ClearBuffer.Color, 0, _transparencyAccumClearColor);
            GL.ClearBuffer(ClearBuffer.Color, 1, _transparencyAccumClearColor);

            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactorSrc.One, BlendingFactorDest.One, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha);

            foreach (var id in _transparentIds) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Render(context, id, in meshData, in renderTarget);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ColorFramebufferHandle);

            // compose transparency

            ref readonly var composeProgram = ref context.Inspect<ShaderProgramData>(Graphics.TransparencyComposeShaderProgramId);
            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, renderTarget.TransparencyAccumTextureHandle);
            GL.Uniform1(composeProgram.CustomLocations["AccumColorTex"], 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, renderTarget.TransparencyAlphaTextureHandle);
            GL.Uniform1(composeProgram.CustomLocations["AccumAlphaTex"], 1);

            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentIds.Clear();
        }

        // render post-processed result

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, renderTarget.ColorTextureHandle);

        if (context.TryGet<RenderTargetDebug>(Graphics.DefaultRenderTargetId, out var debug)) {
            ref readonly var postProgram = ref context.Inspect<ShaderProgramData>(Graphics.PostProcessingDebugShaderProgramId);
            var customLocations = postProgram.CustomLocations;
            GL.UseProgram(postProgram.Handle);

            GL.Uniform1(postProgram.LightsBufferLocation, (int)TextureType.Unknown + 2);
            GL.Uniform1(postProgram.ClustersBufferLocation, (int)TextureType.Unknown + 3);
            GL.Uniform1(postProgram.ClusterLightCountsBufferLocation, (int)TextureType.Unknown + 4);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, renderTarget.TransparencyAccumTextureHandle);
            GL.Uniform1(customLocations["TransparencyAccumBuffer"], 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, renderTarget.TransparencyAlphaTextureHandle);
            GL.Uniform1(customLocations["TransparencyAlphaBuffer"], 2);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, renderTarget.DepthTextureHandle);
            GL.Uniform1(postProgram.DepthBufferLocation, 3);

            var subroutines = postProgram.SubroutineIndeces![Nagule.Graphics.ShaderType.Fragment];
            var subroutineName = debug.DisplayMode switch {
                DisplayMode.TransparencyAccum => "ShowTransparencyAccum",
                DisplayMode.TransparencyAlpha => "ShowTransparencyAlpha",
                DisplayMode.Depth => "ShowDepth",
                DisplayMode.Clusters => "ShowClusters",
                _ => "ShowColor"
            };
            int index = subroutines[subroutineName];
            GL.UniformSubroutines(global::OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader, 1, ref index);
        }
        else {
            ref readonly var postProgram = ref context.Inspect<ShaderProgramData>(Graphics.PostProcessingShaderProgramId);
            var customLocations = postProgram.CustomLocations;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.Points, 0, 1);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(0);
    }

    private void Cull(IContext context, Guid id, in MeshData meshData)
    {
        ref readonly var meshUniformBuffer = ref context.Inspect<MeshUniformBuffer>(id);
        ref readonly var state = ref context.Inspect<MeshRenderingState>(id);

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Mesh, meshUniformBuffer.Handle);
        GL.BindBufferBase(BufferRangeTarget.TransformFeedbackBuffer, 0, meshData.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BindVertexArray(meshData.CullingVertexArrayHandle);

        GL.BeginTransformFeedback(TransformFeedbackPrimitiveType.Points);
        GL.BeginQuery(QueryTarget.PrimitivesGenerated, meshData.CulledQueryHandle);
        GL.DrawArrays(PrimitiveType.Points, 0, state.InstanceCount);
        GL.EndQuery(QueryTarget.PrimitivesGenerated);
        GL.EndTransformFeedback();
    }

    private void Render(IContext context, Guid id, in MeshData meshData, in RenderTargetData renderTarget)
    {
        ref readonly var materialData = ref context.Inspect<MaterialData>(meshData.MaterialId);
        ref readonly var state = ref context.Inspect<MeshRenderingState>(id);

        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObject(meshData.CulledQueryHandle, GetQueryObjectParam.QueryResult, out int visibleCount);

        if (visibleCount > 0) {
            ApplyMaterial(context, in materialData, in renderTarget);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);
        }

        bool materialApplied = false;
        foreach (var variantId in state.VariantIds) {
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Object,
                context.Require<VariantUniformBuffer>(variantId).Handle);
            if (context.TryGet<MaterialData>(variantId, out var overwritingMaterialData)) {
                ApplyMaterial(context, in overwritingMaterialData, in renderTarget);
            }
            else if (!materialApplied) {
                ApplyMaterial(context, in materialData, in renderTarget);
            }
            GL.DrawElements(PrimitiveType.Triangles, meshData.IndexCount, DrawElementsType.UnsignedInt, 0);
        }
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    private void ApplyMaterial(IContext context, in MaterialData materialData, in RenderTargetData renderTarget)
    {
        ref readonly var shaderProgramData = ref context.Inspect<ShaderProgramData>(materialData.ShaderProgramId);

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(shaderProgramData.Handle);

        var textures = materialData.Textures;
        var textureLocations = shaderProgramData.TextureLocations;
        int texCount = (int)TextureType.Unknown;

        for (int i = 0; i != texCount; ++i) {
            int location = textureLocations![i];
            if (location == -1) { continue; };
            var texId = textures[i];
            if (texId == null) { continue; }
            var textureData = context.Inspect<TextureData>(texId.Value);
            GL.ActiveTexture(TextureUnit.Texture1 + i);
            GL.BindTexture(TextureTarget.Texture2D, textureData.Handle);
            GL.Uniform1(location, i + 1);
        }

        if (shaderProgramData.DepthBufferLocation != -1) {
            GL.Uniform1(shaderProgramData.DepthBufferLocation, texCount + 1);
        }
        if (shaderProgramData.LightsBufferLocation != -1) {
            GL.Uniform1(shaderProgramData.LightsBufferLocation, texCount + 2);
        }
        if (shaderProgramData.ClustersBufferLocation != -1) {
            GL.Uniform1(shaderProgramData.ClustersBufferLocation, texCount + 3);
        }
        if (shaderProgramData.ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1(shaderProgramData.ClusterLightCountsBufferLocation, texCount + 4);
        }
    }
}