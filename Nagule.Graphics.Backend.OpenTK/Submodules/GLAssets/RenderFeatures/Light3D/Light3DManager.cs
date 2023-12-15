namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class Light3DManager : GraphicsAssetManagerBase<Light3D, Light3DAsset, Light3DState>
{
    [AllowNull] private Light3DLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Light3DLibrary>();

        Listen((EntityRef entity, in Light3D.SetType cmd) => {
            var type = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                state.Type = type;

                var fType = (float)type;
                _lib.Parameters[state.Index].Type = fType;
                _lib.GetBufferData(state.Index).Type = fType;
                return true;
            });
        });

        Listen((EntityRef entity, in Light3D.SetColor cmd) => {
            var color = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                _lib.Parameters[state.Index].Color = color;
                _lib.GetBufferData(state.Index).Color = color;
                return true;
            });
        });

        Listen((EntityRef entity, in Light3D.SetRange cmd) => {
            var range = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                _lib.Parameters[state.Index].Range = range;
                _lib.GetBufferData(state.Index).Range = range;
                return true;
            });
        });
        
        Listen((EntityRef entity, in Light3D.SetInnerConeAngle cmd) => {
            var angle = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                _lib.Parameters[state.Index].InnerConeAngle = angle;
                _lib.GetBufferData(state.Index).InnerConeAngle = angle;
                return true;
            });
        });

        Listen((EntityRef entity, in Light3D.SetOuterConeAngle cmd) => {
            var angle = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                _lib.Parameters[state.Index].OuterConeAngle = angle;
                _lib.GetBufferData(state.Index).OuterConeAngle = angle;
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Light3D asset)
    {
        var type = asset.Type;
        var color = asset.Color;
        var range = asset.Range;
        var innerConeAngle = asset.InnerConeAngle;
        var OuterConeAngle = asset.OuterConeAngle;

        RenderFrame.Enqueue(entity, () => {
            var state = new Light3DState {
                Type = type,
                Index = _lib.Add(entity, new Light3DParameters {
                    Type = (float)type,
                    Color = color,
                    Range = type switch {
                        LightType.Directional or LightType.Ambient => float.PositiveInfinity,
                        _ => range
                    },
                    InnerConeAngle = innerConeAngle,
                    OuterConeAngle = OuterConeAngle
                })
            };
            RenderStates.Set(entity, state);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Light3D asset)
    {
        RenderFrame.Enqueue(entity, () => {
            if (RenderStates.Remove(entity, out var state)) {
                _lib.Remove(state.Index);
                if (state.Index != _lib.Count) {
                    RenderStates.Get(_lib.Entities[state.Index]).Index = state.Index;
                }
            }
            return true;
        });
    }
}