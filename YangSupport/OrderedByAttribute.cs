namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class OrderedByAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}