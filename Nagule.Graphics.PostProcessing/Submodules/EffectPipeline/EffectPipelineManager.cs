namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Sia;

public partial class EffectPipelineManager
{
    private static readonly string VertShaderSource =
        EmbeddedAssets.LoadInternal<RText>("shaders.postprocessing.vert.glsl");

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        void ReloadEffects(EntityRef entity)
        {
            ref var state = ref entity.GetState<EffectPipelineState>();
            foreach (var effectEntity in state.EffectsRaw.Values) {
                effectEntity.Dispose();
            }

            state.EffectsRaw.Clear();
            state.EffectSequenceRaw.Clear();
            state.MaterialEntity.Dispose();

            LoadEffects(entity, ref state, entity.Get<EffectPipeline>().Effects);
        }
        
        Listen((in EntityRef entity, in EffectPipeline.SetEffects cmd) => ReloadEffects(entity));
        Listen((in EntityRef entity, in EffectPipeline.AddEffect cmd) => ReloadEffects(entity));
        Listen((in EntityRef entity, in EffectPipeline.RemoveEffect cmd) => ReloadEffects(entity));
        Listen((in EntityRef entity, in EffectPipeline.SetEffect cmd) => ReloadEffects(entity));
    }

    public override void LoadAsset(in EntityRef entity, ref EffectPipeline asset, EntityRef stateEntity)
    {
        LoadEffects(entity, ref stateEntity.Get<EffectPipelineState>(), asset.Effects);
    }

    private void LoadEffects(EntityRef entity, ref EffectPipelineState state, ImmutableList<REffectBase> effectRecords)
    {
        var effects = state.EffectsRaw;
        var sequence = state.EffectSequenceRaw;

        foreach (var effect in effectRecords) {
            var effectEntity = World.CreateAssetEntity(effect, entity);
            ref var effectMeta = ref effectEntity.GetState<EffectMetadata>();
            effectMeta.PipelineEntity = entity;

            var entryPoint = effectMeta.EntryPoint;
            try {
                effects.Add(entryPoint, effectEntity);
                sequence.Add(entryPoint);
            }
            catch (ArgumentException) {
                Logger.LogError("Failed to add effect: effect with the same entry point '{EntryPoint}' was found.", entryPoint);
                effectEntity.Dispose();
            }
        }

        state.MaterialEntity = Material.CreateEntity(World, GenerateMaterial(state), entity);
        state.MaterialState = state.MaterialEntity.GetStateEntity();
    }

    private static RMaterial GenerateMaterial(in EffectPipelineState state)
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
        in vec4 Vertex;
        in vec3 EyeDirection;

        out vec4 FragColor;

        """);

        var effects = state.Effects;

        sourceBuilder.AppendLine(
            ShaderUtils.GenerateGLSLPropertiesStatement(
                effects.SelectMany(e =>
                    e.Value.GetState<EffectMetadata>().Properties
                        .Select(p => new MaterialProperty(e.Key + "_" + p.Key, p.Value))),
                (prop, type) => {
                    paramBuilder.Add(prop.Name, type);
                    propBuilder.Add(prop.Name, prop.Value);
                }));

        foreach (var effectName in state.EffectSequence) {
            var effectEntity = effects[effectName];
            sourceBuilder.Append(effectEntity.GetState<EffectMetadata>().Source);
            sourceBuilder.AppendLine();
        }

        sourceBuilder.AppendLine("""
        void main()
        {
            vec3 color = texture(ColorTex, TexCoord).rgb;
            float depth = texture(DepthTex, TexCoord).r;
        """);

        foreach (var effectName in state.EffectSequence) {
            sourceBuilder.Append("    color = ");
            sourceBuilder.Append(effectName);
            sourceBuilder.AppendLine("(color, depth);");
        }

        sourceBuilder.Append("""
            FragColor = vec4(color, 1.0);
        }
        """);

        var shadersBuilder = ImmutableDictionary.CreateBuilder<ShaderType, string>();
        shadersBuilder.Add(ShaderType.Vertex, VertShaderSource);
        shadersBuilder.Add(ShaderType.Fragment, sourceBuilder.ToString());

        return new RMaterial {
            Name = "nagule.postprocessing.effects",
            ShaderProgram = new RGLSLProgram {
                Shaders = shadersBuilder.ToImmutable(),
                Parameters = paramBuilder.ToImmutable()
            },
            Properties = propBuilder.ToImmutable()
        };
    }
}