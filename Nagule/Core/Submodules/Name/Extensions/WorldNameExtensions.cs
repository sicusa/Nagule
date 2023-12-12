namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public static class WorldNameExtensions
{
    public static string GetName(this EntityRef entity)
    {
        ref var nameId = ref entity.Get<Sid<Name>>();
        if (Unsafe.IsNullRef(ref nameId)) {
            return "(null)";
        }
        var name = nameId.Value.Value;
        return name == "" ? "(null)" : name;
    }
}