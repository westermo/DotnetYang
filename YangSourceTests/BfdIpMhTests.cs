using System.Text;
using Ietf.Inet.Types;
using YangSupport;

namespace YangSourceTests;

public class BfdIpMhTests
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

    [Test]
    public async Task NotificationSerializationTest()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var channel = new VoidChannel();
        await notification.Send(channel);
        Console.WriteLine(channel.LastSent);
    }

    [Test]
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
        await Assert.That(newNotification.DestAddr!.Ipv4AddressValue!.WrittenValue)
            .IsEqualTo(notification.DestAddr!.Ipv4AddressValue!.WrittenValue);
        await Assert.That(newNotification.NewState).IsEqualTo(notification.NewState);
        await Assert.That(newNotification.LocalDiscr).IsEqualTo(notification.LocalDiscr);
    }
}