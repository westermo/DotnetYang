namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class TargetAttribute(string xPath) : Attribute
{
    public string XPath { get; } = xPath;
}