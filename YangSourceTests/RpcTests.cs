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
}