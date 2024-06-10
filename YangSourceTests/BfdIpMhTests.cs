using System.Text;
using Ietf.Inet.Types;
using Xunit.Abstractions;
using Yang.Attributes;

namespace YangSourceTests;

public class BfdIpMhTests(ITestOutputHelper output)
{
    private class VoidChannel : IChannel
    {
        public string? LastSent { get; private set; }
        public Task<Stream> Send(string xml)
        {
            LastSent = xml;
            return Task.FromResult(new MemoryStream() as Stream);
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