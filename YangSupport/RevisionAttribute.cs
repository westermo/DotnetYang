namespace YangSupport;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RevisionAttribute(string date) : Attribute
{
    public string Date { get; } = date;
}