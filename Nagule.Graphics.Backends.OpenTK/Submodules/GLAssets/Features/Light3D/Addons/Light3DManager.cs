namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public partial class Light3DManager
{
    private Light3DLibrary _lib = null!;

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
                _lib.ParametersBuffer[state.Index].Type = fType;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetColor cmd) => {
            var color = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].Color = color;
                _lib.ParametersBuffer[state.Index].Color = color;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetRange cmd) => {
            var range = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].Range = range;
                _lib.ParametersBuffer[state.Index].Range = range;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetInnerConeAngle cmd) => {
            var angle = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].InnerConeAngle = angle;
                _lib.ParametersBuffer[state.Index].InnerConeAngle = angle;
            });
        });

        Listen((in EntityRef entity, in Light3D.SetOuterConeAngle cmd) => {
            var angle = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                _lib.Parameters[state.Index].OuterConeAngle = angle;
                _lib.ParametersBuffer[state.Index].OuterConeAngle = angle;
            });
        });
    }

    public override void LoadAsset(in EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        var isEnabled = asset.IsEnabled;
        var type = asset.Type;
        var color = asset.Color;
        var range = asset.Range;
        var innerConeAngle = asset.InnerConeAngle;
        var outerConeAngle = asset.OuterConeAngle;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Light3DState>();
            state = new Light3DState {
                IsEnabled = isEnabled,
                Type = type,
                Index = _lib.Add(stateEntity, new Light3DParameters {
                    Type = (float)type,
                    Color = color,
                    Range = type switch {
                        LightType.Directional or LightType.Ambient => float.PositiveInfinity,
                        _ => range
                    },
                    InnerConeAngle = innerConeAngle,
                    OuterConeAngle = outerConeAngle
                }),
            };
        });
    }

    public override void UnloadAsset(in EntityRef entity, in Light3D asset, EntityRef stateEntity)
    {
        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Light3DState>();
            _lib.Remove(state.Index);
        });
    }
}