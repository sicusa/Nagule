namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using Sia;

public abstract class TextureManagerBase<TTexture, TTextureState>
    : GraphicsAssetManagerBase<TTexture, Bundle<TTextureState, TextureInfo>>
    where TTexture : struct
    where TTextureState : struct, ITextureState
{
    public delegate void StateHandler(ref TTextureState state);

    protected delegate void CommandHandler<TCommand>(ref TTextureState state, in TCommand cmd)
        where TCommand : ICommand<TTexture>;
    
    protected abstract TextureTarget TextureTarget { get; }

    public override void UnloadAsset(in EntityRef entity, in TTexture asset, EntityRef stateEntity)
    {
        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<TTextureState>();
            GL.DeleteTexture(state.Handle.Handle);
        });
    }

    protected void RegisterParameterListener<TCommand>(CommandHandler<TCommand> handler)
        where TCommand : ICommand<TTexture>
    {
        Listen((in EntityRef entity, in TCommand cmd) => {
            var cmdCopy = cmd;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<TTextureState>();
                GL.BindTexture(TextureTarget, state.Handle.Handle);
                handler(ref state, cmdCopy);
                GL.BindTexture(TextureTarget, 0);
            });
        });
    }

    protected void RegisterCommonListeners<
        TSetMinFilterCommand, TSetMagFilterCommand, TSetBorderColorCommand, TSetMipmapEnabledCommand>(
        Func<TSetMinFilterCommand, TextureMinFilter> minFilterGetter,
        Func<TSetMagFilterCommand, TextureMagFilter> magFilterGetter,
        Func<TSetBorderColorCommand, Vector4> borderColorGetter,
        Func<TSetMipmapEnabledCommand, bool> mipmapEnabledGetter)
        where TSetMinFilterCommand : ICommand<TTexture>
        where TSetMagFilterCommand : ICommand<TTexture>
        where TSetBorderColorCommand : ICommand<TTexture>
        where TSetMipmapEnabledCommand : ICommand<TTexture>
    {
        RegisterParameterListener((ref TTextureState state, in TSetMinFilterCommand cmd) => {
            var filter = minFilterGetter(cmd);
            state.MinFilter = filter;
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureMinFilter,
                TextureUtils.Cast(filter, state.MipmapEnabled));
        });

        RegisterParameterListener((ref TTextureState state, in TSetMagFilterCommand cmd) => {
            var filter = magFilterGetter(cmd);
            state.MagFilter = filter;
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureMagFilter, TextureUtils.Cast(filter));
        });
        
        unsafe {
            RegisterParameterListener((ref TTextureState state, in TSetBorderColorCommand cmd) => {
                var borderColor = borderColorGetter(cmd);
                GL.TexParameterf(TextureTarget, TextureParameterName.TextureBorderColor,
                    new ReadOnlySpan<float>(Unsafe.AsPointer(ref borderColor), 4));
            });
        }

        Listen((in EntityRef entity, in TSetMipmapEnabledCommand cmd) => {
            var enabled = mipmapEnabledGetter(cmd);
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<TTextureState>();
                state.MipmapEnabled = enabled;
                if (enabled) {
                    var handle = state.Handle.Handle;
                    GL.BindTexture(TextureTarget, handle);
                    GL.TexParameteri(TextureTarget, TextureParameterName.TextureMinFilter, TextureUtils.Cast(state.MinFilter, enabled));
                    GL.GenerateMipmap(TextureTarget);
                    GL.BindTexture(TextureTarget, 0);
                }
            });
        });
    }

    public void RegenerateTexture(EntityRef entity, Action action)
    {
        var stateEntity = entity.GetStateEntity();

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<TTextureState>();
            GL.BindTexture(TextureTarget, state.Handle.Handle);
            action();
            if (state.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget);
            }
            GL.BindTexture(TextureTarget, 0);
        });
    }

    public void RegenerateTexture(EntityRef entity, StateHandler handler)
    {
        var stateEntity = entity.GetStateEntity();
        if (!stateEntity.Contains<TTextureState>()) {
            return;
        }

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<TTextureState>();

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            handler(ref state);

            if (state.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget);
            }

            GL.BindTexture(TextureTarget, 0);
        });
    }

    protected void SetCommonParameters(TextureMinFilter minFilter, TextureMagFilter magFilter, Vector4 borderColor, bool mipmapEnabled)
    {
        GL.TexParameteri(TextureTarget, TextureParameterName.TextureMinFilter, TextureUtils.Cast(minFilter, mipmapEnabled));
        GL.TexParameteri(TextureTarget, TextureParameterName.TextureMagFilter, TextureUtils.Cast(magFilter));

        unsafe {
            GL.TexParameterf(TextureTarget, TextureParameterName.TextureBorderColor,
                new ReadOnlySpan<float>(Unsafe.AsPointer(ref borderColor), 4));
        }
        if (mipmapEnabled) {
            GL.GenerateMipmap(TextureTarget);
        }
        GL.BindTexture(TextureTarget, 0);
    }

    protected void SetTextureInfo(in EntityRef stateEntity, in TTextureState state)
    {
        ref var info = ref stateEntity.Get<TextureInfo>();
        info.Target = TextureTarget;
        info.Handle = state.Handle;
    }
}