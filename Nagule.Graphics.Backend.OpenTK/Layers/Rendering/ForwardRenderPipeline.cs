namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule;
using Nagule.Graphics;

public class ForwardRenderPipeline : VirtualLayer, ILoadListener, IRenderListener, IWindowResizeListener
{
    private Group<Mesh> _meshGroup = new();
    private Group<Occluder, Mesh> _occluderGroup = new();
    private List<Guid> _delayedMeshes = new();
    private List<Guid> _transparentMeshes = new();

    private int _windowWidth;
    private int _windowHeight;
    private VertexArrayHandle _defaultVertexArray;
    private float[] _transparencyClearColor = {0, 0, 0, 1};

    public void OnLoad(IContext context)
    {
        _defaultVertexArray = GL.GenVertexArray();
    }

    public void OnRender(IContext context, float deltaTime)
    {
        ref var renderTarget = ref context.Acquire<RenderTargetData>(Graphics.DefaultRenderTargetId, out bool exists);
        if (!exists) { return; }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ColorFramebufferHandle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Framebuffer, renderTarget.UniformBufferHandle);
        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown);
        GL.BindTexture(TextureTarget.Texture2d, renderTarget.DepthTextureHandle);

        var lightBufferHandle = context.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 1);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref context.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture1 + (int)TextureType.Unknown + 3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);

        ref readonly var defaultTexData = ref context.Inspect<TextureData>(Graphics.DefaultTextureId);

        // generate hierarchical-Z buffer

        ref readonly var hizProgram = ref context.Inspect<ShaderProgramData>(Graphics.HierarchicalZShaderProgramId);
        int lastMipSizeLocation = hizProgram.CustomParameters["LastMipSize"].Location;
        GL.UseProgram(hizProgram.Handle);

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, renderTarget.DepthTextureHandle);

        int width = renderTarget.Width;
        int height = renderTarget.Height;
        int levelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        for (int i = 1; i < levelCount; ++i) {
            GL.Uniform2i(lastMipSizeLocation, width, height);

            width /= 2;
            height /= 2;
            width = width > 0 ? width : 1;
            height = height > 0 ? height : 1;
            GL.Viewport(0, 0, width, height);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, i - 1);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, i - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, renderTarget.DepthTextureHandle, i);
            GL.DrawArrays(PrimitiveType.Points, 0, 1);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, renderTarget.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, renderTarget.Width, renderTarget.Height);

        // cull instances by camera frustum and occlusion

        var cullProgram = context.Inspect<ShaderProgramData>(Graphics.CullingShaderProgramId);
        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, renderTarget.DepthTextureHandle);

        foreach (var id in _meshGroup.Query(context)) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            Cull(context, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);

        // clear buffers

        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        // render z-buffer

        _occluderGroup.Query(context);

        if (_occluderGroup.Count != 0) {
            GL.ColorMask(false, false, false, false);

            foreach (var id in _occluderGroup) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Render(context, id, in meshData, in renderTarget);
            }

            GL.ColorMask(true, true, true, true);
        }

        // render opaque meshes

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

        foreach (var id in _meshGroup) {
            ref readonly var meshData = ref context.Inspect<MeshData>(id);
            if (meshData.RenderMode == RenderMode.Transparent) {
                _transparentMeshes.Add(id);
                continue;
            }
            if (meshData.RenderMode != RenderMode.Opaque && meshData.RenderMode != RenderMode.Cutoff) {
                _delayedMeshes.Add(id);
                continue;
            }
            Render(context, id, in meshData, in renderTarget);
        }

        // render transparent objects

        if (_transparentMeshes.Count != 0) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.TransparencyFramebufferHandle);
            GL.ClearBufferf(Buffer.Color, 0, _transparencyClearColor);
            GL.ClearBufferf(Buffer.Color, 1, _transparencyClearColor);

            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

            foreach (var id in _transparentMeshes) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                Render(context, id, in meshData, in renderTarget);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ColorFramebufferHandle);

            // compose transparency

            ref readonly var composeProgram = ref context.Inspect<ShaderProgramData>(Graphics.TransparencyComposeShaderProgramId);
            GL.UseProgram(composeProgram.Handle);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, renderTarget.TransparencyAccumTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["AccumTex"].Location, 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, renderTarget.TransparencyRevealTextureHandle);
            GL.Uniform1i(composeProgram.CustomParameters["RevealTex"].Location, 1);

            GL.DrawArrays(PrimitiveType.Points, 0, 1);

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

            _transparentMeshes.Clear();
        }

        // render delayed objects

        if (_delayedMeshes.Count != 0) {
            GL.Enable(EnableCap.Blend);
            GL.DepthMask(false);

            foreach (var id in _delayedMeshes) {
                ref readonly var meshData = ref context.Inspect<MeshData>(id);
                if (meshData.RenderMode == RenderMode.Additive) {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                }
                else if (meshData.RenderMode == RenderMode.Multiplicative) {
                    GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                }
                Render(context, id, in meshData, in renderTarget);
            }
            _delayedMeshes.Clear();

            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);
        }

        // render post-processed result

        GL.Viewport(0, 0, _windowWidth, _windowHeight);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
        GL.BindVertexArray(_defaultVertexArray);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, renderTarget.ColorTextureHandle);

        if (context.TryGet<RenderTargetDebug>(Graphics.DefaultRenderTargetId, out var debug)) {
            ref readonly var postProgram = ref context.Inspect<ShaderProgramData>(Graphics.DebugPostProcessingShaderProgramId);
            var parameters = postProgram.CustomParameters;
            GL.UseProgram(postProgram.Handle);

            GL.Uniform1i(postProgram.LightsBufferLocation, (int)TextureType.Unknown + 2);
            GL.Uniform1i(postProgram.ClustersBufferLocation, (int)TextureType.Unknown + 3);
            GL.Uniform1i(postProgram.ClusterLightCountsBufferLocation, (int)TextureType.Unknown + 4);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, renderTarget.TransparencyAccumTextureHandle);
            GL.Uniform1i(parameters["TransparencyAccumBuffer"].Location, 1);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2d, renderTarget.TransparencyRevealTextureHandle);
            GL.Uniform1i(parameters["TransparencyRevealBuffer"].Location, 2);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2d, renderTarget.DepthTextureHandle);
            GL.Uniform1i(postProgram.DepthBufferLocation, 3);

            var subroutines = postProgram.SubroutineIndeces![Nagule.Graphics.ShaderType.Fragment];
            var subroutineName = debug.DisplayMode switch {
                DisplayMode.TransparencyAccum => "ShowTransparencyAccum",
                DisplayMode.TransparencyAlpha => "ShowTransparencyReveal",
                DisplayMode.Depth => "ShowDepth",
                DisplayMode.Clusters => "ShowClusters",
                _ => "ShowColor"
            };
            uint index = subroutines[subroutineName];
            GL.UniformSubroutinesui(global::OpenTK.Graphics.OpenGL.ShaderType.FragmentShader, 1, index);
        }
        else {
            ref readonly var postProgram = ref context.Inspect<ShaderProgramData>(
                Graphics.PostProcessingShaderProgramId);
            var customLocations = postProgram.CustomParameters;
            GL.UseProgram(postProgram.Handle);
        }

        GL.Disable(EnableCap.DepthTest);
        GL.DrawArrays(PrimitiveType.Points, 0, 1);
        GL.Enable(EnableCap.DepthTest);

        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    private void Cull(IContext context, Guid id, in MeshData meshData)
    {
        ref readonly var meshUniformBuffer = ref context.Inspect<MeshUniformBuffer>(id);
        ref readonly var state = ref context.Inspect<MeshRenderingState>(id);

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshUniformBuffer.Handle);
        GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, meshData.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BindVertexArray(meshData.CullingVertexArrayHandle);

        GL.BeginTransformFeedback(PrimitiveType.Points);
        GL.BeginQuery(QueryTarget.PrimitivesGenerated, meshData.CulledQueryHandle);
        GL.DrawArrays(PrimitiveType.Points, 0, state.InstanceCount);
        GL.EndQuery(QueryTarget.PrimitivesGenerated);
        GL.EndTransformFeedback();
    }

    private void Render(IContext context, Guid meshId, in MeshData meshData, in RenderTargetData renderTarget)
    {
        var matId = meshData.MaterialId;

        ref readonly var materialData = ref context.Inspect<MaterialData>(matId);
        ref readonly var state = ref context.Inspect<MeshRenderingState>(meshId);

        if (materialData.IsTwoSided) {
            GL.Disable(EnableCap.CullFace);
        }

        int visibleCount = 0;
        GL.BindVertexArray(meshData.VertexArrayHandle);
        GL.GetQueryObjecti(meshData.CulledQueryHandle, QueryObjectParameterName.QueryResult, ref visibleCount);

        if (visibleCount > 0) {
            ApplyMaterial(context, matId, in materialData, in renderTarget);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, visibleCount);
        }

        bool materialApplied = false;
        foreach (var variantId in state.VariantIds) {
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Object,
                context.Require<VariantUniformBuffer>(variantId).Handle);
            if (context.TryGet<MaterialData>(variantId, out var overwritingMaterialData)) {
                ApplyMaterial(context, matId, in overwritingMaterialData, in renderTarget);
            }
            else if (!materialApplied) {
                ApplyMaterial(context, matId, in materialData, in renderTarget);
            }
            GL.DrawElements(PrimitiveType.Triangles, meshData.IndexCount, DrawElementsType.UnsignedInt, 0);
        }

        if (materialData.IsTwoSided) {
            GL.Enable(EnableCap.CullFace);
        }
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    private void ApplyMaterial(IContext context, Guid id, in MaterialData materialData, in RenderTargetData renderTarget)
    {
        ref readonly var shaderProgramData = ref context.Inspect<ShaderProgramData>(materialData.ShaderProgramId);

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialData.Handle);
        GL.UseProgram(shaderProgramData.Handle);

        const int texCount = (int)TextureType.Unknown;
        var textures = materialData.Textures;
        var textureLocations = shaderProgramData.TextureLocations;

        for (int i = 0; i != texCount; ++i) {
            int location = textureLocations![i];
            if (location == -1) { continue; };
            var texId = textures[i];
            if (texId == null) {
                GL.Uniform1i(location, 0);
                continue;
            }
            var textureData = context.Inspect<TextureData>(texId.Value);
            GL.ActiveTexture(TextureUnit.Texture1 + (uint)i);
            GL.BindTexture(TextureTarget.Texture2d, textureData.Handle);
            GL.Uniform1i(location, i + 1);
        }

        if (shaderProgramData.DepthBufferLocation != -1) {
            GL.Uniform1i(shaderProgramData.DepthBufferLocation, texCount + 1);
        }
        if (shaderProgramData.LightsBufferLocation != -1) {
            GL.Uniform1i(shaderProgramData.LightsBufferLocation, texCount + 2);
        }
        if (shaderProgramData.ClustersBufferLocation != -1) {
            GL.Uniform1i(shaderProgramData.ClustersBufferLocation, texCount + 3);
        }
        if (shaderProgramData.ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1i(shaderProgramData.ClusterLightCountsBufferLocation, texCount + 4);
        }

        if (context.TryGet<MaterialSettings>(id, out var settings)) {
            foreach (var (name, value) in settings.Parameters) {
                if (shaderProgramData.CustomParameters.TryGetValue(name, out var par)) {
                    try {
                        GLHelper.SetUniform(par.Type, par.Location, value);
                    }
                    catch (Exception e) {
                        Console.WriteLine($"Failed to set material parameter '{name}': " + e.Message);
                    }
                }
            }
        }
    }
}