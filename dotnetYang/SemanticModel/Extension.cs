using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Extension : Statement
{
    public Extension(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "extension";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(YangParser.SemanticModel.Argument.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
    ];

    public override string ToCode()
    {
        var arg = Children.FirstOrDefault(child => child is Argument);
        var vargName = arg?.Argument;
        if (vargName == Argument)
        {
            if (Argument != "value") vargName = "value";
            else vargName = "_" + vargName;
        }

        var argStr = vargName is null
            ? string.Empty
            : $$"""
                public string {{MakeName(vargName)}} { get; set; }
                public {{MakeName(Argument)}}(string {{MakeName(vargName)}})
                {
                    this.{{MakeName(vargName)}} = {{MakeName(vargName)}};
                }
                """;
        return $$"""
                 public class {{MakeName(Argument)}}
                 {
                     {{Indent(argStr)}}
                 }
                 """;
    }
}