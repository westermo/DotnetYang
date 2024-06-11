namespace YangSupport;

[AttributeUsage(AttributeTargets.Class)]
public class PresenceAttribute(string meaning) : Attribute
{
    public string Meaning { get; } = meaning;
}