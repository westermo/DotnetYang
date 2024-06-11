namespace YangSupport;

public class InstanceIdentifier(string path)
{
    public string Path { get; } = path;
    public static InstanceIdentifier Parse(string id) => new(id);
    public override string ToString() => Path;
}