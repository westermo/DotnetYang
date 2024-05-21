using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class FeatureFlag : Statement
{
    public FeatureFlag(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "if-feature";

    public override string ToCode()
    {
        Parent?.Attributes.Add($"IfFeature(\"{Argument}\")");
        return string.Empty;
    }
}