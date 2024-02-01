namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public partial class Light3DManager
{
    [AllowNull] private Light3DLibrary _lib;
    [AllowNull] private ShadowMapLibrary _shadowMapLib;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _lib = world.GetAddon<Light3DLibrary>();
        _shadowMapLib = world.GetAddon<ShadowMapLibrary>();

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

        Listen((in EntityRef entity, in Light3D.SetIsShadowEnabled cmd) => {
            var enabled = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            var prevEnabled = _shadowMapLib.Contains(entity);
            if (prevEnabled == enabled) {
                return;
            }

            ShadowMapHandle? handle = null;

            if (enabled) {
                var newHandle = _shadowMapLib.Allocate(entity);
                if (newHandle != null) {
                    handle = newHandle.Value;
                }
            }
            else {
                _shadowMapLib.Release(entity);
            }

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Light3DState>();
                state.ShadowMapHandle = handle;

                var handleNum = handle?.Value ?? -1f;
                _lib.Parameters[state.Index].ShadowMapIndex = handleNum;
                _lib.ParametersBuffer[state.Index].ShadowMapIndex = handleNum;

                if (handle.HasValue) {
                    state.ShadowMapFramebufferHandle = CreateShadowMapFramebuffer(handle.Value);
                }
                else {
                    GL.DeleteFramebuffer(state.ShadowMapFramebufferHandle.Handle);
                    state.ShadowMapFramebufferHandle = FramebufferHandle.Zero;
                }
            });
        });

        Listen((in EntityRef entity, in Light3D.SetShadowStrength cmd) => {
            var strength = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        var isEnabled = asset.IsEnabled;
        var type = asset.Type;
        var color = asset.Color;
        var range = asset.Range;
        var innerConeAngle = asset.InnerConeAngle;
        var outerConeAngle = asset.OuterConeAngle;
        var handle = asset.IsShadowEnabled ? _shadowMapLib.Allocate(entity) : null;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Light3DState>();
            state = new Light3DState {
                IsEnabled = isEnabled,
                Type = type,
                Index = _lib.Add(entity, new Light3DParameters {
                    Type = (float)type,
                    Color = color,
                    Range = type switch {
                        LightType.Directional or LightType.Ambient => float.PositiveInfinity,
                        _ => range
                    },
                    InnerConeAngle = innerConeAngle,
                    OuterConeAngle = outerConeAngle
                }),
                ShadowMapHandle = handle,
                ShadowMapFramebufferHandle =
                    handle.HasValue ? CreateShadowMapFramebuffer(handle.Value) : FramebufferHandle.Zero
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

    private FramebufferHandle CreateShadowMapFramebuffer(ShadowMapHandle shadowMapHandle)
    {
        static void DoBind(ShadowMapLibrary shadowMapLib, ShadowMapHandle shadowMapHandle, int framebufferHandle)
        {
            int prevFramebuffer = 0;
            GL.GetInteger(GetPName.DrawFramebufferBinding, ref prevFramebuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferHandle);
            GL.FramebufferTextureLayer(
                FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                shadowMapLib.TilesetState.Handle.Handle, 0, shadowMapHandle.Value);
                
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFramebuffer);
        }

        var handle = GL.GenFramebuffer();

        if (_shadowMapLib.ShadowMapTilesetState.Valid && _shadowMapLib.TilesetState.Loaded) {
            DoBind(_shadowMapLib, shadowMapHandle, handle);
        }
        else {
            RenderFramer.Start(() => {
                if (!_shadowMapLib.TilesetState.Loaded) {
                    return false;
                }
                DoBind(_shadowMapLib, shadowMapHandle, handle);
                return true;
            });
        }
        return new(handle);
    }
}