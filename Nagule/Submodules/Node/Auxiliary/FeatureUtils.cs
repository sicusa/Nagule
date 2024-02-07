using System.Reflection;
using System.Runtime.InteropServices;

namespace Nagule;

public static class FeatureUtils
{
    private static readonly ThreadLocal<Dictionary<Type, Type[]>> s_requiredFeatureDict = new(() => []);

    public static Type[] GetRequiredFeatures(Type featureRecordType)
    {
        ref var types = ref CollectionsMarshal.GetValueRefOrAddDefault(
            s_requiredFeatureDict.Value!, featureRecordType, out bool exists);
        
        if (exists) {
            return types!;
        }

        types = featureRecordType.GetCustomAttributes(typeof(NaRequireFeatureAttribute<>))
            .Select(attr => ((INaRequireFeatureAttribute)attr).FeatureType)
            .ToArray();
        return types;
    }
}