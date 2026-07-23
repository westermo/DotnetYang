namespace YangSupport;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MustAttribute(string xPath) : Attribute
{
    public string XPath { get; } = xPath;
    public string? ErrorTag { get; set; }
    public string? ErrorMessage { get; set; }
}