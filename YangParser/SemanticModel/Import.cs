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

        Revision = Children.FirstOrDefault(child => child is RevisionDate)?.Argument;
        Prefix = Children.First(child => child is Prefix).Argument;
        var reference = Children.FirstOrDefault(child => child is Reference);
        var description = Children.FirstOrDefault(child => child is Description);
        AdditionalInformation = reference is null ? string.Empty : $"Reference: \"{reference.Argument}\", ";
        AdditionalInformation += description is null ? string.Empty : $"Description: \"{description}\"";
    }

    private readonly string? Revision;
    private readonly string Prefix;
    private readonly string? AdditionalInformation;

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