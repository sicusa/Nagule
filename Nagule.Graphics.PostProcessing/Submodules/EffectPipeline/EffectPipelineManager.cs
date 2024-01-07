namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Sia;

public partial class EffectPipelineManager
{
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
        
        Listen((EntityRef entity, in EffectPipeline.SetEffects cmd) => ReloadEffects(entity));
        Listen((EntityRef entity, in EffectPipeline.AddEffect cmd) => ReloadEffects(entity));
        Listen((EntityRef entity, in EffectPipeline.RemoveEffect cmd) => ReloadEffects(entity));
        Listen((EntityRef entity, in EffectPipeline.SetEffect cmd) => ReloadEffects(entity));
    }

    protected override void LoadAsset(EntityRef entity, ref EffectPipeline asset, EntityRef stateEntity)
    {
        LoadEffects(entity, ref stateEntity.Get<EffectPipelineState>(), asset.Effects);
    }

    private void LoadEffects(EntityRef entity, ref EffectPipelineState state, ImmutableList<REffectBase> effectRecords)
    {
        var effects = state.EffectsRaw;
        var sequence = state.EffectSequenceRaw;

        foreach (var effect in effectRecords) {
            var effectEntity = AssetSystemModule.UnsafeCreateEntity(World, effect, entity);
            var entryPoint = effectEntity.GetState<EffectMetadata>().EntryPoint;
            try {
                effects.Add(entryPoint, effectEntity);
                sequence.Add(entryPoint);
            }
            catch (ArgumentException) {
                Logger.LogError("Failed to add effect: effect with the same entry point '{EntryPoint}' was found.", entryPoint);
                effectEntity.Destroy();
            }
        }

        state.MaterialEntity = Material.CreateEntity(World, GenerateMaterial(state), entity);
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
        """);

        foreach (var effectName in state.EffectSequence) {
            sourceBuilder.Append("    color = ");
            sourceBuilder.Append(effectName);
            sourceBuilder.AppendLine("(color);");
        }

        sourceBuilder.Append("""
            FragColor = vec4(color, 1.0);
        }
        """);

        var shadersBuilder = ImmutableDictionary.CreateBuilder<ShaderType, string>();
        shadersBuilder.Add(ShaderType.Vertex, ShaderUtils.LoadCore("nagule.common.quad.vert.glsl"));
        shadersBuilder.Add(ShaderType.Fragment, sourceBuilder.ToString());

        return new RMaterial {
            Name = "PostProcessing",
            ShaderProgram = new RGLSLProgram {
                Shaders = shadersBuilder.ToImmutable(),
                Parameters = paramBuilder.ToImmutable()
            },
            Properties = propBuilder.ToImmutable()
            
        };
    }
}