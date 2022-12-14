namespace Nagule;

using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

public class NameRegisterer : VirtualLayer, IUpdateListener
{
    private Query<Modified<Name>, Name> _q = new();

    public void OnUpdate(IContext context)
    {
        bool librarySet = false;
        ref var library = ref Unsafe.NullRef<NameLookupLibrary>();

        foreach (var id in _q.Query(context)) {
            if (!librarySet) {
                library = ref context.AcquireAny<NameLookupLibrary>();
                librarySet = true;
            }

            var dict = library.Dictionary;
            var name = context.Inspect<Name>(id).Value;

            if (context.TryGet<AppliedName>(id, out var appliedName)) {
                var prevName = appliedName.Value;
                if (prevName == name) {
                    continue;
                }
                if (dict.TryGetValue(prevName, out var prevSet) && prevSet.Remove(id)) {
                    if (prevSet.Count == 0) {
                        dict.Remove(prevName);
                    }
                }
            }

            if (!dict.TryGetValue(name, out var set)) {
                set = new HashSet<Guid>();
                dict.Add(name, set);
            }

            set.Add(id);
            context.Acquire<AppliedName>(id).Value = name;
        }
    }
}