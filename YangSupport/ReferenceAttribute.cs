namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ReferenceAttribute(string reference) : Attribute
{
    public string Reference { get; } = reference;
}