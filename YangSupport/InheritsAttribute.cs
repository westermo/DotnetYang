namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class InheritsAttribute(string baseName) : Attribute
{
    public string BaseName { get; } = baseName;
}