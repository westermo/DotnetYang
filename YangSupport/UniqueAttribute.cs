namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class UniqueAttribute(params string[] value) : Attribute
{
    public string[] Value { get; } = value;
}