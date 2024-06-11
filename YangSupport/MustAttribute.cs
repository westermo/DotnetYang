namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MustAttribute(string xPath) : Attribute
{
    public string XPath { get; } = xPath;
}