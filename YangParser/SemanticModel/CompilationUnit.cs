using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class CompilationUnit : Statement
{
    public CompilationUnit(Module[] modules) : base(new YangStatement(String.Empty, string.Empty, [],
        new Metadata(string.Empty, new Parser.Position(), 0)))
    {
        Children = modules;
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Module.Keyword, Cardinality.ZeroOrMore),
    ];

    public override string ToCode()
    {
        return string.Join("\n", Children.Select(c => c.ToCode()));
    }
}