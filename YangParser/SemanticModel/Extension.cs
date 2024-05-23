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
        var argStr = arg is null
            ? string.Empty
            : $$"""
                public string {{MakeName(arg.Argument)}} { get; set; }
                public {{MakeName(Argument)}}Attribute(string {{MakeName(arg.Argument)}})
                {
                    this.{{MakeName(arg.Argument)}} = {{MakeName(arg.Argument)}};
                }
                """;
        return $$"""
                 [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                 public class {{MakeName(Argument)}}Attribute : Attribute
                 {
                     {{Indent(argStr)}}
                 }
                 """;
    }
}