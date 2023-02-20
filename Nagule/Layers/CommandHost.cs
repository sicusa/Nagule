namespace Nagule;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Aeco;
using Aeco.Local;

public class CommandHost : CompositeLayer, ICommandHost
{
    public IEnumerable<KeyValuePair<string, Profile>> Profiles => _parent.Profiles;

    private IContext _parent;

    public CommandHost(IContext parent, params ILayer<IComponent>[] sublayers)
        : base()
    {
        _parent = parent;

        InternalAddSublayers(
            new PolySingletonStorage<ISingletonComponent>(),
            new PolyTagStorage<ITagComponent>(),
            new PolyHashStorage<IHashComponent>(),
            new PolyDenseStorage<IPooledComponent>());
    }

    public void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => _parent.SendCommand<TTarget>(command);

    public void SendCommandBatched<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => _parent.SendCommandBatched<TTarget>(command);

    public bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget
        => _parent.TryGetCommand<TTarget>(out command);

    public ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget
        => _parent.WaitCommand<TTarget>();

    public IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget
        => _parent.ConsumeCommands<TTarget>();

    public void SubmitBatchedCommands()
        => _parent.SubmitBatchedCommands();

    public Profile? GetProfile(string path)
        => _parent.GetProfile(path);
    public Profile? GetProfile(string category, object target)
        => _parent.GetProfile(category, target);

    public bool RemoveProfile(string path)
        => _parent.RemoveProfile(path);
    public bool RemoveProfile(string path, [MaybeNullWhen(false)] out Profile profile)
        => _parent.RemoveProfile(path, out profile);
    public void ClearProfiles()
        => _parent.ClearProfiles();

    public IDisposable Profile(string path)
        => _parent.Profile(path);
    public IDisposable Profile(string category, object target)
        => _parent.Profile(category, target);
}