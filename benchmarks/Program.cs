﻿using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Ietf.Inet.Types;
using Yang.Attributes;
using YangParser;
using YangParser.Parser;
using YangParser.SemanticModel;
using YangSource;

namespace Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
public class ParsingBenchmarks
{
    private string source;
    private YangStatement statement;
    private IStatement model;
    private Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification notification;
    private string serialized;
    private IYangServer server;
    private IChannel channel;

    private static readonly Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput input =
        new()
        {
            MaNameStringValue = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.MaNameString(),
            MdNameStringValue = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.MdNameString(),
            Ttl = 2,
            Count = 4,
            Interval = 6,
            CosId = 2,
            DestinationMep = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.DestinationMepContainer
            {
                MepAddress =
                    new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.DestinationMepContainer.MepAddressChoice
                    {
                        IpAddressCaseValue =
                            new Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput.DestinationMepContainer.
                                MepAddressChoice.IpAddressCaseValueCase
                                {
                                    IpAddress = new YangNode.IpAddress(new YangNode.Ipv4Address("1.2.3.4"))
                                }
                    }
            },
            SourceMepValue = new(),
            CommandSubType = Ietf.Connection.Oriented.Oam.YangNode.CommandSubTypeIdentity.Proactive
        };

    [GlobalSetup]
    public void Setup()
    {
        source = File.ReadAllText("../../../../lin.yang");
        statement = Parser.Parse("lin.yang", source);
        model = StatementFactory.Create(statement);
        notification = new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("1.2.3.4")),
            NewState = Ietf.Bfd.Types.YangNode.State.Init,
            SourceAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("2.3.4.5"))
        };
        serialized = notification.ToXML().Result;
        server = new ExampleYangServer();
        channel = new BenchmarkingChannel(server);
    }

    [Benchmark]
    public YangStatement Parse() => Parser.Parse("lin.yang", source);

    [Benchmark]
    public IStatement SemanticModel() => StatementFactory.Create(statement);

    [Benchmark]
    public string ToCode() => model.ToCode();

    [Benchmark]
    public Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification MultihopNotificationCreation() =>
        new()
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("1.2.3.4")),
            NewState = Ietf.Bfd.Types.YangNode.State.Init,
            SourceAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("2.3.4.5"))
        };

    [Benchmark]
    public async Task<string> SerializerMultihopNotification() => await notification.ToXML();

    [Benchmark]
    public MemoryStream ToMemoryStream()
    {
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
        ms.Dispose();
        return ms;
    }

    [Benchmark]
    public async Task<Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification> MultihopNotificationParsing()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(serialized));
        return await Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification.ParseAsync(ms);
    }

    [Benchmark]
    public async Task<Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput> TracerouteRoundTrip()
    {
        return await Ietf.Connection.Oriented.Oam.YangNode.Traceroute(channel, 123, input);
    }
}

internal class BenchmarkingChannel(IYangServer server) : IChannel
{
    public async Task<Stream> Send(string xml)
    {
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var output = new MemoryStream();
        await server.Receive(input, output);
        output.Seek(0, SeekOrigin.Begin);
        return output;
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>();
    }
}