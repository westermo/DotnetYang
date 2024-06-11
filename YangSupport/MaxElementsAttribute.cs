namespace YangSupport;

[AttributeUsage(AttributeTargets.All)]
public class MaxElementsAttribute(int value) : Attribute
{
    public int Value { get; } = value;
}