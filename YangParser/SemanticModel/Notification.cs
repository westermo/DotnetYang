using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Notification : NodeDataStatement, IXMLParseable
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

    private string? _className;
    public bool IsTopLevel => Root == this;

    public string ServerDeclaration => "Task On" + ClassName + "(" + ParsedType +
                                       " notification, global::System.DateTime eventTime);";


    public string ReceiveCase => IsTopLevel
        ? $"case \"{XmlObjectName}\" when reader.NamespaceURI is \"{Namespace}\":\n" + $$"""
              {
                  var input = await {{ParsedType}}.ParseAsync(reader);
                  await server.On{{ClassName}}(input, eventTime);
                  break;
              }
              """
        : $$"""
            if({{TargetPath}} != null) {
                await server.On{{ClassName}}({{Root.TargetName}}, eventTime);
                break;
            }
            """;

    private string ParsedType => IsTopLevel ? FullyQualifiedNamespace() + "." + ClassName : QualifiedRootName;

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var xmlWrite = GetXmlWriting();
        var xmlRead = GetXmlReading();
        var addRoot = IsTopLevel ? string.Empty : $", {QualifiedRootName} source";
        return $$"""
                 public {{ClassName}}? {{TargetName}};
                 {{DescriptionString}}{{AttributeString}}
                 public class {{ClassName}}
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     public async Task Send(IChannel channel{{addRoot}})
                     {
                         StringBuilder stringBuilder = new StringBuilder();
                         using XmlWriter writer = XmlWriter.Create(stringBuilder, SerializationHelper.GetStandardWriterSettings());
                         await writer.WriteStartElementAsync(null,"notification","urn:ietf:params:xml:ns:netconf:notification:1.0");
                         await writer.WriteStartElementAsync(null,"eventTime",null);
                         await writer.WriteStringAsync(DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ"));
                         await writer.WriteEndElementAsync();
                         {{xmlWrite}}
                         await writer.WriteEndElementAsync();
                         await writer.FlushAsync();
                         var response = await channel.Send(stringBuilder.ToString());
                         response.Dispose();
                     }
                     public static async Task<{{ParsedType}}> ParseAsync(global::System.IO.Stream xmlStream)
                     {
                         using XmlReader reader = XmlReader.Create(xmlStream,SerializationHelper.GetStandardReaderSettings());
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.Element || reader.Name != "notification" || reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:notification:1.0")
                         {
                             throw new Exception($"Expected stream to start with a <notification> element with namespace \"urn:ietf:params:xml:ns:netconf:notification:1.0\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
                         }
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.Element || reader.Name != "eventTime")
                         {
                             throw new Exception($"Expected stream to have a second element called <eventTime> element but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.Text)
                         {
                             if(!global::System.DateTime.TryParse(await reader.GetValueAsync(), out _)) throw new Exception($"Expected <eventTime> element to contain a valid dateTime but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.EndElement)
                         {
                             throw new Exception($"Expected <eventTime> element to only have one child but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         var value = {{xmlRead}}
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.EndElement)
                         {
                             throw new Exception($"Expected </notification> closing element {reader.NodeType}: {reader.Name}");
                         }
                         return value;
                     }
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                 }
                 """;
    }

    private string GetXmlWriting()
    {
        return IsTopLevel ? "await WriteXMLAsync(writer);" : "await source.WriteXMLAsync(writer);";
    }

    private string GetXmlReading()
    {
        return IsTopLevel ? "await ParseAsync(reader);" : $"await {QualifiedRootName}.ParseAsync(reader);";
    }

    private string? _targetName;
    public override string? TargetName => _targetName ??= ClassName + "Instance";
    public override string ClassName => _className ??= MakeName(Argument);
}