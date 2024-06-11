namespace YangSupport;

public interface IChannel : IDisposable
{
    Stream WriteStream { get; }
    Stream ReadStream { get; }
    Task Send();
}