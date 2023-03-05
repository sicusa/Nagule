namespace Nagule;

using System.Diagnostics.CodeAnalysis;

public interface IProfiler
{
    IEnumerable<KeyValuePair<string, Profile>> Profiles { get; }

    Profile? GetProfile(string path);
    Profile? GetProfile(string category, object target);

    IObservable<Profile> ObserveProfile(string path);
    IObservable<Profile> ObserveProfile(string path, object target);

    bool RemoveProfile(string path);
    bool RemoveProfile(string path, [MaybeNullWhen(false)] out Profile profile);

    void ClearProfiles();

    IDisposable Profile(string path);
    IDisposable Profile(string category, object target);
}