using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Notification : Statement
{
    public Notification(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "notification";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var xmlWrite = GetXmlWriting();
        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public class {{MakeName(Argument)}}
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     public async Task<string> ToXML()
                     {
                         XmlWriterSettings settings = new XmlWriterSettings();
                         settings.Indent = true;
                         settings.OmitXmlDeclaration = true;
                         settings.NewLineOnAttributes = true;
                         settings.Async = true;
                         StringBuilder stringBuilder = new StringBuilder();
                         using XmlWriter writer = XmlWriter.Create(stringBuilder, settings);
                         await writer.WriteStartElementAsync(null,"notification","urn:ietf:params:xml:ns:netconf:notification:1.0");
                         await writer.WriteStartElementAsync(null,"eventTime",null);
                         await writer.WriteStringAsync(DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"));
                         await writer.WriteEndElementAsync();
                         await writer.WriteStartElementAsync("{{XmlNamespace?.Prefix}}","{{Argument}}","{{XmlNamespace?.Namespace}}");
                         {{xmlWrite}}
                         await writer.WriteEndElementAsync();
                         await writer.WriteEndElementAsync();
                         return stringBuilder.ToString();
                     }
                     {{Indent(XmlFunction())}}
                 }
                 """;
    }

    private string GetXmlWriting()
    {
        if (Parent is Module)
        {
            return "await WriteXML(writer);";
        }

        var parent = Parent;
        while (parent != null)
        {
            if (parent.Parent is Module or Submodule)
            {
                break;
            }

            parent = parent.Parent;
        }

        if (parent is IXMLSource source)
        {
            return $"await WriteXML(writer);";
        }

        throw new SemanticError(
            $"Top level statement of 'notification {Argument}' ({parent?.Source.Keyword} {parent?.Argument}) was not a valid XML source",
            Source);
    }
}