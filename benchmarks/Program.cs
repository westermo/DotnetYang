using System.Text;
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

    private static readonly Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.SetOperatorStateInput
        SetOperatorStateInput = new()
        {
            State = Ietf.Alarms.YangNode.WritableOperatorState.Ack,
            Text = "Acked"
        };

    private static readonly Ietf.Alarms.YangNode.AlarmsContainer alarmsContainer = new()
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

    private class VoidChannel : IChannel
    {
        public string LastSent { get; private set; }

        public Task<Stream> Send(string xml)
        {
            LastSent = xml;
            return Task.FromResult(new MemoryStream() as Stream);
        }
    }

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
        var tmp = new VoidChannel();
        notification.Send(tmp).Wait();
        serialized = tmp.LastSent;
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
    public async Task<string> SerializerMultihopNotification()
    {
        var tmp = new VoidChannel();
        await notification.Send(tmp);
        return tmp.LastSent;
    }

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

    [Benchmark]
    public async Task SetOperatorStateRoundTrip()
    {
        await alarmsContainer.AlarmList!.Alarm![0]
            .SetOperatorState(channel, 123, alarmsContainer, SetOperatorStateInput);
    }

    [Benchmark]
    public async Task NotificationRoundTrip()
    {
        await notification.Send(channel);
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