namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public partial class Light3DManager
{
    [AllowNull] private Light3DLibrary _lib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<Light3DLibrary>();

        Listen((in EntityRef entity, in Light3D.SetType cmd) => {
            var type = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                state.Type = type;

                var fType = (float)type;
                _lib.Parameters[state.Index].Type = fType;
                _lib.GetBufferData(state.Index).Type = fType;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetColor cmd) => {
            var color = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].Color = color;
                _lib.GetBufferData(state.Index).Color = color;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetRange cmd) => {
            var range = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].Range = range;
                _lib.GetBufferData(state.Index).Range = range;
            });
        });
        
        Listen((in EntityRef entity, in Light3D.SetInnerConeAngle cmd) => {
            var angle = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].InnerConeAngle = angle;
                _lib.GetBufferData(state.Index).InnerConeAngle = angle;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetOuterConeAngle cmd) => {
            var angle = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].OuterConeAngle = angle;
                _lib.GetBufferData(state.Index).OuterConeAngle = angle;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        var type = asset.Type;
        var color = asset.Color;
        var range = asset.Range;
        var innerConeAngle = asset.InnerConeAngle;
        var OuterConeAngle = asset.OuterConeAngle;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Light3DState>();
            state = new Light3DState {
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
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Light3DState>();
            _lib.Remove(state.Index);
            if (state.Index != _lib.Count) {
                _lib.Entities[state.Index].GetState<Light3DState>().Index = state.Index;
            }
        });
    }
}