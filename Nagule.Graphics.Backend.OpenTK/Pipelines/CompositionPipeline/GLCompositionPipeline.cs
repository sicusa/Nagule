namespace Nagule.Graphics.Backend.OpenTK;

using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Local;

public class GLCompositionPipeline : PolyHashStorage<IComponent>, ICompositionPipeline
{
    public IReadOnlyList<ICompositionPass> Passes => _passes;
    public Guid RenderSettingsId { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Material Material { get; private set; } = Material.Default;
    public Guid MaterialId { get; private set; }

    private List<ICompositionPass> _passes;
    private List<IExecutableCompositionPass> _executablePasses;

    private Guid _id = Guid.NewGuid();
    private bool _initialized;
    private string _profileKey;
    private static int s_pipelineCounter;

    public event Action<ICommandHost, ICompositionPipeline>? OnResize;

    public GLCompositionPipeline(Guid renderSettingsId, IEnumerable<ICompositionPass> passes)
    {
        RenderSettingsId = renderSettingsId;

        _passes = new(passes);
        _executablePasses = new(_passes.OfType<IExecutableCompositionPass>());

        _profileKey = "CompositionPipeline_" + s_pipelineCounter++;
    }

    public void LoadResources(IContext context)
    {
        Material = MergePasses(_passes);
        MaterialId = context.GetResourceLibrary().Reference(_id, Material);

        foreach (var pass in _passes) {
            pass.LoadResources(context);
        }
    }

    private Material MergePasses(IEnumerable<ICompositionPass> passes)
    {
        var sourceBuilder = new StringBuilder();
        var paramBuilder = ImmutableDictionary.CreateBuilder<string, ShaderParameterType>();
        var propBuilder = ImmutableDictionary.CreateBuilder<string, Dyn>();

        paramBuilder.Add("ColorTex", ShaderParameterType.Texture2D);
        paramBuilder.Add("DepthTex", ShaderParameterType.Texture2D);

        sourceBuilder.AppendLine("""
        #version 410 core

        uniform sampler2D ColorTex;
        uniform sampler2D DepthTex;

        in vec2 TexCoord;
        out vec4 FragColor;
        """);

        sourceBuilder.AppendLine(
            GraphicsHelper.GenerateGLSLPropertiesStatement(
                passes.SelectMany(p => p.Properties),
                (prop, type) => {
                    paramBuilder.Add(prop.Name, type);
                    propBuilder.Add(prop.Name, prop.Value);
                }));

        foreach (var pass in passes) {
            if (pass.Source == null) {
                continue;
            }
            sourceBuilder.Append(pass.Source);
            sourceBuilder.AppendLine();
        }

        sourceBuilder.AppendLine("""
        void main()
        {
            vec3 color;
        """);

        foreach (var pass in passes) {
            if (pass.EntryPoint == null) {
                continue;
            }
            sourceBuilder.Append("    color = ");
            sourceBuilder.Append(pass.EntryPoint);
            sourceBuilder.AppendLine("(color);");
        }

        sourceBuilder.Append("""
            FragColor = vec4(color, 1.0);
        }
        """);

        var shadersBuilder = ImmutableDictionary.CreateBuilder<ShaderType, string>();
        shadersBuilder.Add(ShaderType.Vertex, GraphicsHelper.LoadEmbededShader("nagule.common.quad.vert.glsl"));
        shadersBuilder.Add(ShaderType.Fragment, sourceBuilder.ToString());

        return new Material {
            Name = _profileKey,
            ShaderProgram = new GLSLProgram {
                Shaders = shadersBuilder.ToImmutable(),
                Parameters = paramBuilder.ToImmutable()
            },
            Properties = propBuilder.ToImmutable()
        };
    }

    public void UnloadResources(IContext context)
    {
        foreach (var pass in _passes) {
            pass.UnloadResources(context);
        }
        context.GetResourceLibrary().UnreferenceAll(_id);
    }

    public void Initialize(ICommandHost host)
    {
        if (_initialized) {
            throw new InvalidOperationException("Composiiton pipeline has been initialized");
        }
        _initialized = true;

        foreach (var pass in _passes) {
            try {
                pass.Initialize(host, this);
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to initialize pass '{pass}': " + e);
            }
        }
    }

    public void Uninitialize(ICommandHost host)
    {
        if (!_initialized) {
            throw new InvalidOperationException("Composiiton pipeline has not been initialized");
        }
        _initialized = false;

        foreach (var pass in _passes) {
            try {
                pass.Uninitialize(host, this);
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to uninitialize pass '{pass}': " + e);
            }
        }
    }

    public void Execute(ICommandHost host, IRenderPipeline renderPipeline, FramebufferHandle targetFramebuffer)
    {
        ref var materialData = ref host.RequireOrNullRef<MaterialData>(MaterialId);
        if (Unsafe.IsNullRef(ref materialData)) { return; }

        if (renderPipeline.ColorTextureHandle is TextureHandle colorTex) {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, colorTex);
        }
        if (renderPipeline.DepthTextureHandle is TextureHandle depthTex) {
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, depthTex);
        }

        foreach (var pass in CollectionsMarshal.AsSpan(_executablePasses)) {
            try {
                using (host.Profile(_profileKey, pass)) {
                    pass.Execute(host, this, renderPipeline);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to execute pass '{pass}': " + e);
            }
        }

        using (host.Profile(_profileKey, "FinalPass")) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFramebuffer);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, renderPipeline.UniformBufferHandle);
            
            ref var programData = ref GLHelper.ApplyInternalMaterial(host, MaterialId, in materialData);
            var texLocations = programData.TextureLocations!;

            if (texLocations.TryGetValue("ColorTex", out var loc)) {
                GL.Uniform1i(loc, 0);
            }
            if (texLocations.TryGetValue("DepthTex", out loc)) {
                GL.Uniform1i(loc, 1);
            }

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GLHelper.DrawQuad();
        }
    }
    public void Resize(ICommandHost host, int width, int height)
    {
        Width = width;
        Height = height;
        OnResize?.Invoke(host, this);
    }
}