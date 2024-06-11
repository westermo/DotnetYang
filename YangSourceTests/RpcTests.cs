using System.Text;
using System.Xml;
using Ietf.Inet.Types;
using Xunit.Abstractions;
using YangSupport;
using YangSource;

namespace YangSourceTests;

public class RpcTests(ITestOutputHelper outputHelper)
{
    private class TestChannel : IChannel, IAsyncDisposable
    {
        public string? LastXML => Encoding.UTF8.GetString(((MemoryStream)WriteStream).GetBuffer()).Replace("\0","");
        public string? LastWritten => Encoding.UTF8.GetString(((MemoryStream)ReadStream).GetBuffer()).Replace("\0","");
        private ExampleYangServer server = new();

        public async Task Send()
        {
            (LastReadIndex, WriteStream.Position) = (WriteStream.Position, LastReadIndex);
            LastSentIndex = ReadStream.Position;
            await server.Receive(WriteStream, ReadStream);
            ReadStream.Position = LastSentIndex;
            (LastReadIndex, WriteStream.Position) = (WriteStream.Position, LastReadIndex);
        }

        private long LastSentIndex = 0;
        private long LastReadIndex = 0;
        public Stream WriteStream { get; } = new MemoryStream();
        public Stream ReadStream { get; } = new MemoryStream();

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
    public async Task RpcSend()
    {
        await using var channel = new TestChannel();
        var id = Random.Shared.Next();
        var reply = await Ietf.Connection.Oriented.Oam.YangNode.Traceroute(channel, id,
            new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput
            {
                MaNameStringValue = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.MaNameString(),
                MdNameStringValue = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.MdNameString(),
                Ttl = 2,
                Count = 4,
                Interval = 6,
                CosId = 2,
                DestinationMep = new()
                {
                    MepAddress = new()
                    {
                        IpAddressCaseValue = new()
                        {
                            IpAddress = new(new YangNode.Ipv4Address("1.2.3.4"))
                        }
                    }
                },
                SourceMepValue = new(),
                CommandSubType = Ietf.Connection.Oriented.Oam.YangNode.CommandSubTypeIdentity.Proactive
            });
        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
        using var ms = new MemoryStream();
        await using var writer = XmlWriter.Create(ms, SerializationHelper.GetStandardWriterSettings());
        await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
        await writer.WriteAttributeStringAsync(null, "message-id", null, id.ToString());
        await reply.WriteXMLAsync(writer);
        await writer.WriteEndElementAsync();
        await writer.FlushAsync();
        var replyString = Encoding.UTF8.GetString(ms.GetBuffer());
        Assert.Equal(channel.LastWritten, replyString);
        // Assert.Equal(
        //     reply.Response![0].Mip!.MipAddress!.IpAddressCaseValue!.IpAddress!.Ipv4AddressValue!
        //         .WrittenValue,
        //     output.Response![0].Mip!.MipAddress!.IpAddressCaseValue!.IpAddress!.Ipv4AddressValue!
        //         .WrittenValue);
        // Assert.Equal(
        //     reply.Response![1].Ttl,
        //     output.Response![1].Ttl);
    }

    private static readonly Ietf.Alarms.YangNode.AlarmsContainer root = new()
    {
        AlarmList = new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer
        {
            Alarm =
            [
                new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry
                {
                    TimeCreated = "2015-01-23T12:23:34Z",
                    Resource = "something",
                    AlarmTypeId = Ietf.Alarms.YangNode.AlarmTypeIdIdentity.AlarmTypeId,
                    IsCleared = false,
                    LastRaised = "2014-01-23T12:23:34Z",
                    LastChanged = "2014-01-22T12:23:34Z",
                    PerceivedSeverity = Ietf.Alarms.YangNode.Severity.Critical,
                    AlarmText = "boo",
                },
                new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry
                {
                    TimeCreated = "2015-01-23T12:25:34Z",
                    Resource = "something",
                    AlarmTypeId = Ietf.Alarms.YangNode.AlarmTypeIdIdentity.AlarmTypeId,
                    IsCleared = false,
                    LastRaised = "2014-01-23T12:28:34Z",
                    LastChanged = "2014-01-22T12:22:34Z",
                    PerceivedSeverity = Ietf.Alarms.YangNode.Severity.Critical,
                    AlarmText = "baa",
                    OperatorActionInstance =
                        new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.OperatorAction
                        {
                            Operator = "Me",
                            State = new Ietf.Alarms.YangNode.OperatorState(Ietf.Alarms.YangNode.OperatorState
                                .OperatorState1.Shelved),
                            Time = "2014-01-23T12:28:34Z",
                            Text = "Some fine message"
                        }
                }
            ]
        }
    };

    [Fact]
    public async Task ActionSend()
    {
        
        await using var channel = new TestChannel();
        await root.AlarmList!.Alarm![0].SetOperatorState(channel, Random.Shared.Next(), root,
            new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.SetOperatorStateInput
            {
                State = Ietf.Alarms.YangNode.WritableOperatorState.Ack,
                Text = "Acked"
            });

        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
    }

    [Fact]
    public async Task NotificationSend()
    {
        
        await using var channel = new TestChannel();
        await root.AlarmList!.Alarm![1].OperatorActionInstance!.Send(channel, root);

        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
    }
    [Fact]
    public async Task TopLevelNotificationSend()
    {
        await using var channel = new TestChannel();
        var notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("192.168.0.1")),
            NewState = Ietf.Bfd.Types.YangNode.State.AdminDown
        };
        await notification.Send(channel);
        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
    }
    [Fact]
    public async Task ExceptionGeneratingTest()
    {
        
        await using var channel = new TestChannel();
        var notification = new Ietf.Alarms.YangNode.AlarmNotification 
        {
            Resource = "a",
            Time = "2015-01-23T12:23:34Z",
            AlarmText = "a",
            PerceivedSeverity = Ietf.Alarms.YangNode.SeverityWithClear.SeverityWithClear0.Cleared,
            AlarmTypeId = new Ietf.Alarms.YangNode.AlarmTypeId(Ietf.Alarms.YangNode.AlarmTypeIdIdentity.AlarmTypeId)
        };
        await notification.Send(channel);
        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
        Assert.Contains("rpc-error", channel.LastWritten);
    }

    [Fact]
    public async Task ExceptionThrowingTest()
    {
        
        await using var channel = new TestChannel();
        try
        {
            var result = await Ietf.Subscribed.Notifications.YangNode.EstablishSubscription(channel, Random.Shared.Next(),
                new Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionInput
                {
                    Target = new Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionInput.TargetChoice
                    {
                        StreamCaseValue =
                            new Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionInput.TargetChoice.
                                StreamCaseValueCase
                                {
                                    StreamFilter =
                                        new Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionInput.
                                            TargetChoice.StreamCaseValueCase.StreamFilterChoice
                                            {
                                                ByReferenceCaseValue =
                                                    new Ietf.Subscribed.Notifications.YangNode.
                                                        EstablishSubscriptionInput.TargetChoice.StreamCaseValueCase.
                                                        StreamFilterChoice.ByReferenceCaseValueCase()
                                                        {
                                                            StreamFilterName =
                                                                new Ietf.Subscribed.Notifications.YangNode.
                                                                    StreamFilterRef()
                                                        }
                                            }
                                }
                    }
                });
        }
        catch (RpcException e)
        {
            outputHelper.WriteLine(e.Message);
            Assert.True(true);
            return;
        }
        catch (Exception)
        {
            outputHelper.WriteLine(channel.LastXML);
            outputHelper.WriteLine("_____________________________________");
            outputHelper.WriteLine(channel.LastWritten);
            throw;
        }
        Assert.Fail();
    }
}