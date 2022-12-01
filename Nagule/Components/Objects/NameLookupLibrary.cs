namespace Nagule;

using System.Collections.Immutable;

public struct NameLookupLibrary : ISingletonComponent
{
    public ImmutableDictionary<string, HashSet<Guid>> Dictionary =
        ImmutableDictionary<string, HashSet<Guid>>.Empty;
    
    public NameLookupLibrary() {}

    public static Guid? Find(IContext context, string name)
    {
        var dict = context.AcquireAny<NameLookupLibrary>(out bool exists).Dictionary;
        return exists && dict.TryGetValue(name, out var ids) && ids.Count > 0 ? ids.First() : null;
    }

    public static IEnumerable<Guid> FindAll(IContext context, string name)
    {
        var dict = context.AcquireAny<NameLookupLibrary>(out bool exists).Dictionary;
        return exists && dict.TryGetValue(name, out var ids) ? ids : Enumerable.Empty<Guid>();
    }
}