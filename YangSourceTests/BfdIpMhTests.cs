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
    public async Task NotificationEventTime_IsCurrentUtcTimeInIso8601()
    {
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        var channel = new VoidChannel();
        var before = DateTime.UtcNow;
        await notification.Send(channel);
        var after = DateTime.UtcNow;

        var match = System.Text.RegularExpressions.Regex.Match(channel.LastSent!, "<eventTime>([^<]*)</eventTime>");
        await Assert.That(match.Success).IsTrue();
        var parsed = DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd'T'HH:mm:ss'Z'",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        // The timestamp has whole-second precision, so allow for truncation of 'before'.
        await Assert.That(parsed).IsGreaterThanOrEqualTo(before.AddSeconds(-1));
        await Assert.That(parsed).IsLessThanOrEqualTo(after);
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