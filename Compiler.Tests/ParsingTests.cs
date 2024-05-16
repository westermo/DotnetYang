using System.Text;
using Xunit.Abstractions;
using YangParser;
using YangParser.SemanticModel;

namespace Compiler.Tests;

public class ParsingTests(ITestOutputHelper output)
{
    [Fact]
    public void IetfYangLibrary()
    {
        var result = Parser.Parse("memory", File.ReadAllText("lin.yang"));
        var statements = StatementFactory.Create(result);
        Print(statements);
    }

    private void Print(YangStatement statement, int indent = 0)
    {
        var terminator = statement.Children.Count == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.WriteLine($"{tabs}{statement.Prefix}{statement.Keyword} {statement.Argument}{terminator}");
        if (statement.Children.Count <= 0) return;
        output.WriteLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
        output.WriteLine($"{tabs}}}");
    }

    private void Print(IStatement statement, int indent = 0)
    {
        var terminator = statement.Children.Length == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.WriteLine($"{tabs}{statement.GetType().Name} {statement.Argument}{terminator}");
        if (statement.Children.Length <= 0) return;
        output.WriteLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
        output.WriteLine($"{tabs}}}");
    }
}