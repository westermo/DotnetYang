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
}