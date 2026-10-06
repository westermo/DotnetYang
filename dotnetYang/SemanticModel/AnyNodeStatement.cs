using YangParser.Parser;

namespace YangParser.SemanticModel;

/// <summary>
/// Shared implementation for the opaque 'anydata' and 'anyxml' data nodes, which
/// are both surfaced as a raw XML string property.
/// </summary>
public abstract class AnyNodeStatement : Statement, IXMLWriteValue, IXMLReadValue
{
    protected AnyNodeStatement(YangStatement statement, string keyword) : base(statement)
    {
        if (statement.Keyword != keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {keyword}", statement);
    }

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
        foreach (var child in Children)
        {
            child.ToCode();
        }

        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public string? {{TargetName}} { get; set; }
                 """;
    }

    public string TargetName => MakeName(Argument);
    public string ClassName => "string";

    public string WriteCall =>
        $$"""
          if({{TargetName}} != null)
          {
              await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
              await writer.WriteStringAsync({{TargetName}});
              await writer.WriteEndElementAsync();
          }
          """;

    public string ParseCall =>
        $$"""
          _{{TargetName}} = reader.ReadInnerXml();
          """;
}
