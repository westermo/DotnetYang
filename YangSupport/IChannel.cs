namespace YangSupport;

public interface IChannel
{
    Task<Stream> Send(string xml);
}