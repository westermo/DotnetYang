namespace YangSupport;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProvidesFeatureAttribute(string flag) : Attribute
{
    public string FeatureFlag { get; } = flag;
}