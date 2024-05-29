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
    private IStatement model;

    [GlobalSetup]
    public void Setup()
    {
        source = File.ReadAllText("../../../../lin.yang");
        statement = Parser.Parse("lin.yang", source);
        model = StatementFactory.Create(statement);
    }

    [Benchmark]
    public YangStatement Parse() => Parser.Parse("lin.yang", source);

    [Benchmark]
    public IStatement SemanticModel() => StatementFactory.Create(statement);

    [Benchmark]
    public string ToCode() => model.ToCode();
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ParsingBenchmarks>();
    }
}