namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class MinElementsAttribute(int value) : Attribute
{
    public int Value { get; } = value;
}