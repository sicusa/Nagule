namespace Nagule;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Sia;

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

    public static void GuardRequiredFeatures(
        IEnumerable<Type> requiredFeatureTypes,
        IEnumerable<Type>? satisfiedFeatureTypes = null)
    {
        var unsatisfiedFeatureTypes = satisfiedFeatureTypes == null
            ? requiredFeatureTypes
            : requiredFeatureTypes.Except(satisfiedFeatureTypes);

        StringBuilder? unsatisfiedFeatureNames = null;

        foreach (var featureType in unsatisfiedFeatureTypes) {
            unsatisfiedFeatureNames ??= new();
            unsatisfiedFeatureNames.Append(featureType);
            unsatisfiedFeatureNames.Append(", ");
        }

        if (unsatisfiedFeatureNames != null) {
            var msg = unsatisfiedFeatureNames.Remove(unsatisfiedFeatureNames.Length - 2, 2);
            throw new InvalidOperationException($"Following features are required: " + msg);
        }
    }

    public static EntityRef? CreateEntity(
        World world, RFeatureBase record, EntityRef nodeEntity,
        IEnumerable<Type>? satisfiedFeatureTypes = null)
    {
        var featureEntity = RawCreateEntity(world, record, nodeEntity, satisfiedFeatureTypes);
        if (featureEntity == null) {
            return null;
        }
        nodeEntity.Get<NodeFeatures>().Add(featureEntity.Value, record);
        return featureEntity;
    }

    internal static EntityRef? RawCreateEntity(
        World world, RFeatureBase record, EntityRef nodeEntity,
        IEnumerable<Type>? satisfiedFeatureTypes = null)
    {
        var recordType = record.GetType();
        var requiredFeatureTypes = GetRequiredFeatures(recordType);
        if (requiredFeatureTypes.Length != 0) {
            GuardRequiredFeatures(requiredFeatureTypes, satisfiedFeatureTypes);
        }

        var featureEntity = world.CreateAsset(
            record, Bundle.Create(new Feature(nodeEntity, record.IsEnabled)), AssetLife.Persistent);

        if (!featureEntity.Valid) {
            return null;
        }
        featureEntity.Get<Feature>()._self = featureEntity;
        return featureEntity;
    }
}