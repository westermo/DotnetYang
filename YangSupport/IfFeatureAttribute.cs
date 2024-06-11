namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class IfFeatureAttribute(string flag) : Attribute
{
    public string FeatureFlag { get; } = flag;
}