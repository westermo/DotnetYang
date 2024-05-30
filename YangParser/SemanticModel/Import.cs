using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Import : Statement
{
    public Import(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "import";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(SemanticModel.Prefix.Keyword, Cardinality.Required),
        new ChildRule(RevisionDate.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public override string ToCode()
    {
        return string.Empty;
    }
}