namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class WhenAttribute(string xPath) : Attribute
{
    public string XPath { get; } = xPath;
}