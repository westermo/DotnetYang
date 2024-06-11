namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class UniqueAttribute(params string[] value) : Attribute
{
    public string[] Value { get; } = value;
}