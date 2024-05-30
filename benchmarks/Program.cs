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
    }

    [Benchmark]
    public YangStatement Parse() => Parser.Parse("lin.yang", source);

    [Benchmark]
    public IStatement SemanticModel() => StatementFactory.Create(statement);

    [Benchmark]
    public string ToCode() => model.ToCode();

    [Benchmark]
    public Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification MultihopNotificationCreation() =>
        new Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification
        {
            DestAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("1.2.3.4")),
            NewState = Ietf.Bfd.Types.YangNode.State.Init,
            SourceAddr = new YangNode.IpAddress(new YangNode.Ipv4Address("2.3.4.5"))
        };

    [Benchmark]
    public async Task<string> SerializerMultihopNotification() => await notification.ToXML();
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>();
    }
}