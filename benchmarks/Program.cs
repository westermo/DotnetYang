using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Ietf.Inet.Types;
using YangParser;
using YangParser.Parser;
using YangParser.SemanticModel;

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
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>();
    }
}