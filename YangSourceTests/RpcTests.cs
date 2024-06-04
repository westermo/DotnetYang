using System.Text;
using System.Xml;
using Ietf.Inet.Types;
using Xunit.Abstractions;
using Yang.Attributes;

namespace YangSourceTests;

public class RpcTests(ITestOutputHelper outputHelper)
{
    private static readonly Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput output = new()
    {
        Response = new List<Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry>
        {
            new()
            {
                Mip = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MipContainer
                {
                    MipAddress =
                        new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MipContainer.
                            MipAddressChoice
                            {
                                IpAddressCaseValue =
                                    new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.
                                        MipContainer.MipAddressChoice.IpAddressCaseValueCase
                                        {
                                            IpAddress = new YangNode.IpAddress(
                                                new YangNode.Ipv4Address("12.23.34.45"))
                                        }
                            }
                }
            },
            new()
            {
                Ttl = 1,
                MonitorStats =
                    new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MonitorStatsChoice
                    {
                        MonitorNullCaseValue =
                            new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.
                                MonitorStatsChoice.
                                MonitorNullCaseValueCase
                                {
                                    MonitorNull = new object()
                                }
                    }
            }
        }
    };

    private class TestChannel : IChannel
    {
        public string? LastXML { get; private set; }
        public string? LastWritten { get; private set; }

        public async Task<Stream> Send(string xml)
        {
            LastXML = xml;
            var stream = new MemoryStream();
            await using var writer = XmlWriter.Create(stream, SerializationHelper.GetStandardWriterSettings());
            await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
            await writer.WriteAttributeStringAsync(null, "message-id", null, MessageID.ToString());
            await output.WriteXMLAsync(writer);
            await writer.WriteEndElementAsync();
            await writer.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);
            LastWritten = Encoding.UTF8.GetString(stream.GetBuffer());
            return stream;
        }

        public int MessageID { get; set; }
    }

    [Fact]
    public async Task RpcSend()
    {
        var channel = new TestChannel();
        channel.MessageID = Random.Shared.Next();
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
        Assert.Equal(
            reply.Response![0].Mip!.MipAddress!.IpAddressCaseValue!.IpAddress!.Ipv4AddressValue!
                .WrittenValue,
            output.Response![0].Mip!.MipAddress!.IpAddressCaseValue!.IpAddress!.Ipv4AddressValue!
                .WrittenValue);
        Assert.Equal(
            reply.Response![1].Ttl,
            output.Response![1].Ttl);
    }
}