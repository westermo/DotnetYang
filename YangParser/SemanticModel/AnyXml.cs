using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class AnyXml : Statement
{
    public AnyXml(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "anyxml";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Config.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(When.Keyword)
    ];

    public override string ToCode()
    {
        return $"public string? {TargetName} {{ get; set; }}";
    }

    public string TargetName => MakeName(Argument);

    public string WriteCall
    {
        get
        {
            return $$"""
                     if({{TargetName}} != null)
                     {
                         await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                         await writer.WriteStringAsync({{TargetName}});
                         await writer.WriteEndElementAsync();
                     }
                     """;
        }
    }
}