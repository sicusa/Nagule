namespace Nagule;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Aeco;
using Aeco.Local;

public class CommandHost : CompositeLayer, ICommandHost
{
    public IEnumerable<KeyValuePair<string, Profile>> Profiles => _context.Profiles;

    private IContext _context;

    public CommandHost(IContext parent, params ILayer<IComponent>[] sublayers)
        : base()
    {
        _context = parent;

        InternalAddSublayers(
            new PolySingletonStorage<ISingletonComponent>(),
            new PolyTagStorage<ITagComponent>(),
            new PolyHashStorage<IHashComponent>(),
            new PolyDenseStorage<IPooledComponent>());
    }

    public void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => _context.SendCommand<TTarget>(command);

    public void SendCommandBatched<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => _context.SendCommandBatched<TTarget>(command);

    public bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget
        => _context.TryGetCommand<TTarget>(out command);

    public ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget
        => _context.WaitCommand<TTarget>();

    public IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget
        => _context.ConsumeCommands<TTarget>();

    public void SubmitBatchedCommands()
        => _context.SubmitBatchedCommands();

    public Profile? GetProfile(string path)
        => _context.GetProfile(path);
    public Profile? GetProfile(string category, object target)
        => _context.GetProfile(category, target);

    public IObservable<Profile> ObserveProfile(string path)
        => _context.ObserveProfile(path);
    public IObservable<Profile> ObserveProfile(string path, object target)
        => _context.ObserveProfile(path, target);

    public bool RemoveProfile(string path)
        => _context.RemoveProfile(path);
    public bool RemoveProfile(string path, [MaybeNullWhen(false)] out Profile profile)
        => _context.RemoveProfile(path, out profile);
    public void ClearProfiles()
        => _context.ClearProfiles();

    public IDisposable Profile(string path)
        => _context.Profile(path);
    public IDisposable Profile(string category, object target)
        => _context.Profile(category, target);
}