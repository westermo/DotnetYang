using System.Text;
using Ietf.Inet.Types;
using Xunit.Abstractions;

namespace YangSourceTests;

public class BfdIpMhTests(ITestOutputHelper output)
{
    [Fact]
    public async Task NotificationSerializationTest()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var result = await notification.ToXML();
        output.WriteLine(result);
    }

    [Fact]
    public async Task NotificationDeserializationTest()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var result = await notification.ToXML();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(result));
        var newNotification = await Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification.ParseAsync(ms);
        Assert.Equal(notification.DestAddr!.Ipv4AddressValue!.WrittenValue,
            newNotification.DestAddr!.Ipv4AddressValue!.WrittenValue);
        Assert.Equal(notification.NewState, newNotification.NewState);
        Assert.Equal(notification.LocalDiscr, newNotification.LocalDiscr);
    }
}