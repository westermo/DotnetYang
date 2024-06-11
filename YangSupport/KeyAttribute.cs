namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class KeyAttribute(params string[] value) : Attribute
{
    public string[] Value { get; } = value;
}