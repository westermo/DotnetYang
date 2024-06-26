using System.Text;
using Ietf.Inet.Types;
using Xunit.Abstractions;
using YangSupport;

namespace YangSourceTests;

public class BfdIpMhTests(ITestOutputHelper output)
{
    private class VoidChannel : IChannel, IAsyncDisposable
    {
        public string? LastSent { get; private set; }
        public Stream WriteStream { get; } = new MemoryStream();
        public Stream ReadStream { get; } = new MemoryStream();

        public Task Send()
        {
            LastSent = Encoding.UTF8.GetString(((MemoryStream)WriteStream).GetBuffer());
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            WriteStream.Dispose();
            ReadStream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await WriteStream.DisposeAsync();
            await ReadStream.DisposeAsync();
        }
    }

    [Fact]
    public async Task NotificationSerializationTest()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var channel = new VoidChannel();
        await notification.Send(channel);
        output.WriteLine(channel.LastSent);
    }

    [Fact]
    public async Task NotificationDeserializationTest()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var channel = new VoidChannel();
        await notification.Send(channel);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(channel.LastSent!));
        var newNotification = await Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification.ParseAsync(ms);
        Assert.Equal(notification.DestAddr!.Ipv4AddressValue!.WrittenValue,
            newNotification.DestAddr!.Ipv4AddressValue!.WrittenValue);
        Assert.Equal(notification.NewState, newNotification.NewState);
        Assert.Equal(notification.LocalDiscr, newNotification.LocalDiscr);
    }
}