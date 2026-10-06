using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Ietf.Inet.Types;
using YangSupport;
using YangParser.Parser;
using YangParser.SemanticModel;
using YangSource;
using Path = System.IO.Path;

namespace Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private string source = null!;
    private YangStatement statement = null!;
    private IStatement model = null!;
    private Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification notification = null!;
    private string serialized = null!;
    private IYangServer server = null!;
    private IChannel channel = null!;

    private static readonly Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput input =
        new()
        {
            MaNameString = "Mama String",
            MdNameString = "Maddy String",
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
            SourceMep = "Meppity",
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
            Alarm = new YangSupport.YangList<(Ietf.Alarms.YangNode.Resource, Ietf.Alarms.YangNode.AlarmTypeId, Ietf.Alarms.YangNode.AlarmTypeQualifier), Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry>(e => (e.Resource!, e.AlarmTypeId, e.AlarmTypeQualifier!))
            {
                new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry
                {
                    TimeCreated = "2015-01-23T12:23:34Z",
                    Resource = "something",
                    AlarmTypeId = Ietf.Alarms.YangNode.AlarmTypeIdIdentity.AlarmTypeId,
                    AlarmTypeQualifier = "",
                    IsCleared = false,
                    LastRaised = "2014-01-23T12:23:34Z",
                    LastChanged = "2014-01-22T12:23:34Z",
                    PerceivedSeverity = Ietf.Alarms.YangNode.Severity.Critical,
                    AlarmText = "boo"
                },
                new Ietf.Alarms.YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry
                {
                    TimeCreated = "2015-01-23T12:25:34Z",
                    Resource = "something2",
                    AlarmTypeId = Ietf.Alarms.YangNode.AlarmTypeIdIdentity.AlarmTypeId,
                    AlarmTypeQualifier = "",
                    IsCleared = false,
                    LastRaised = "2014-01-23T12:28:34Z",
                    LastChanged = "2014-01-22T12:22:34Z",
                    PerceivedSeverity = Ietf.Alarms.YangNode.Severity.Critical,
                    AlarmText = "baa"
                }
            }
        }
    };

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

    [GlobalSetup]
    public void Setup()
    {
        source = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "lin.yang"));
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
        serialized = tmp.LastSent!.Replace("\0","");
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
        await using var tmp = new VoidChannel();
        await notification.Send(tmp);
        return tmp.LastSent!;
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

internal class BenchmarkingChannel(IYangServer server) : IChannel, IAsyncDisposable
{
    public Stream WriteStream { get; } = new MemoryStream();
    public Stream ReadStream { get; } = new MemoryStream();

    // Each Send is a complete round trip, so both streams are truncated to keep memory bounded
    // across the millions of invocations BenchmarkDotNet performs.
    public async Task Send()
    {
        WriteStream.Position = 0;
        ReadStream.SetLength(0);
        await server.Receive(WriteStream, ReadStream);
        WriteStream.SetLength(0);
        ReadStream.Position = 0;
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

internal static class Program
{
    private static int Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>(args: args);
        var failed = summary.Reports.Where(r => !r.Success || r.ResultStatistics is null)
            .Select(r => r.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo)
            .ToList();
        if (summary.HasCriticalValidationErrors || failed.Count > 0)
        {
            Console.Error.WriteLine($"Benchmarks failed: {string.Join(", ", failed)}");
            return 1;
        }

        return 0;
    }
}