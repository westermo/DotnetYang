using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
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

    [GlobalSetup]
    public void Setup()
    {
        source = File.ReadAllText("../../../../lin.yang");
        statement = Parser.Parse("lin.yang", source);
    }

    [Benchmark]
    public YangStatement Parse() => Parser.Parse("lin.yang", source);

    [Benchmark]
    public IStatement SemanticModel() => StatementFactory.Create(statement);
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>();
    }
}