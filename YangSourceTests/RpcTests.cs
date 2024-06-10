using System.Text;
using System.Xml;
using Ietf.Inet.Types;
using Xunit.Abstractions;
using Yang.Attributes;
using YangSource;

namespace YangSourceTests;

public class RpcTests(ITestOutputHelper outputHelper)
{
    private class TestChannel : IChannel
    {
        public string? LastXML { get; private set; }
        public string? LastWritten { get; private set; }
        private ExampleYangServer server = new();

        public async Task<Stream> Send(string xml)
        {
            using var input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            LastXML = xml;
            var output = new MemoryStream();
            await server.Receive(input, output);
            output.Seek(0, SeekOrigin.Begin);
            LastWritten = Encoding.UTF8.GetString(output.GetBuffer());
            return output;
        }

        public int MessageID { get; set; }
    }

    [Fact]
    public async Task RpcSend()
    {
        var channel = new TestChannel
        {
            MessageID = Random.Shared.Next()
        };
        var reply = await Ietf.Connection.Oriented.Oam.YangNode.Traceroute(channel, channel.MessageID,
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
        await writer.WriteAttributeStringAsync(null, "message-id", null, channel.MessageID.ToString());
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

    [Fact]
    public async Task ActionSend()
    {
        var channel = new TestChannel
        {
            MessageID = Random.Shared.Next()
        };
        
        var root = new Ietf.Alarms.YangNode.AlarmsContainer
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
                        AlarmText = "boo"
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
                        AlarmText = "baa"
                    }
                ]
            }
        };
        await root.AlarmList.Alarm[0].SetOperatorState(channel,channel.MessageID,root,new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.SetOperatorStateInput
        {
            State = Ietf.Alarms.YangNode.WritableOperatorState.Ack,
            Text = "Acked"
        });
        
        outputHelper.WriteLine(channel.LastXML);
        outputHelper.WriteLine("_____________________________________");
        outputHelper.WriteLine(channel.LastWritten);
    }
}