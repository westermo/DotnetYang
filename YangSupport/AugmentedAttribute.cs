namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class AugmentedAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}