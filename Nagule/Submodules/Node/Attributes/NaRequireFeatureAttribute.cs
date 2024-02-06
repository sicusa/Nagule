namespace Nagule;

public interface INaRequireFeatureAttribute
{
    Type FeatureType { get; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class NaRequireFeatureAttribute<TFeature> : Attribute, INaRequireFeatureAttribute
    where TFeature : IAssetRecord
{
    public Type FeatureType => typeof(TFeature);
}