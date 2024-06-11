namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class XPathAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}