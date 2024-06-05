using System;
using System.Collections.Generic;
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

    private string FullyQualifiedNamespace()
    {
        var parent = Parent;
        List<string> classChain = new();
        while (parent is not Module && parent is not null)
        {
            switch (parent)
            {
                case IXMLParseable xml:
                    classChain.Insert(0, xml.ClassName);
                    break;
                case IXMLReadValue readValue:
                    classChain.Insert(0, readValue.ClassName);
                    break;
            }

            parent = parent.Parent;
        }

        if (parent is Module module)
        {
            classChain.Insert(0, "YangNode");
            classChain.Insert(0, MakeNamespace(module.Argument));
        }

        return string.Join(".", classChain);
    }

    public string ServerDeclaration => "Task On" + MakeName(Argument) + "(" +
                                       FullyQualifiedNamespace() + "." + MakeName(Argument) +
                                       " notification, global::System.DateTime eventTime);";


    public string ReceiveCase => $"case \"{Argument}\" when reader.NamespaceURI is \"{Namespace}\":\n" + $$"""
          {
              var input = await {{FullyQualifiedNamespace() + "." + MakeName(Argument)}}.ParseAsync(reader);
              await server.On{{MakeName(Argument)}}(input, eventTime);
              break;
          }
          """;

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var xmlWrite = GetXmlWriting();
        var xmlRead = GetXmlReading();
        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public class {{MakeName(Argument)}}
                 {
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     public async Task<string> ToXML()
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
                         return stringBuilder.ToString();
                     }
                     public static async Task<{{MakeName(Argument)}}> ParseAsync(global::System.IO.Stream xmlStream)
                     {
                         using XmlReader reader = XmlReader.Create(xmlStream,SerializationHelper.GetStandardReaderSettings());
                         await reader.ReadAsync();
                         if(reader.NodeType != XmlNodeType.Element || reader.Name != "notification" || reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:notification:1.0")
                         {
                             throw new Exception($"Expected stream to start with a <notification> element with namespace \"urn:ietf:params:xml:ns:netconf:notification:1.0\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
                         }
                         await reader.ReadAsync();
                         while(reader.NodeType == XmlNodeType.Whitespace)
                         {
                             await reader.ReadAsync();
                         }
                         if(reader.NodeType != XmlNodeType.Element || reader.Name != "eventTime")
                         {
                             throw new Exception($"Expected stream to have a second element called <eventTime> element but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         while(reader.NodeType == XmlNodeType.Whitespace)
                         {
                             await reader.ReadAsync();
                         }
                         if(reader.NodeType != XmlNodeType.Text)
                         {
                             if(!global::System.DateTime.TryParse(await reader.GetValueAsync(), out _)) throw new Exception($"Expected <eventTime> element to contain a valid dateTime but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         while(reader.NodeType == XmlNodeType.Whitespace)
                         {
                             await reader.ReadAsync();
                         }
                         if(reader.NodeType != XmlNodeType.EndElement)
                         {
                             throw new Exception($"Expected <eventTime> element to only have one child but got {reader.NodeType}: {reader.Name}");
                         }
                         await reader.ReadAsync();
                         while(reader.NodeType == XmlNodeType.Whitespace)
                         {
                             await reader.ReadAsync();
                         }
                         var value = {{xmlRead}}
                         await reader.ReadAsync();
                         while(reader.NodeType == XmlNodeType.Whitespace)
                         {
                             await reader.ReadAsync();
                         }
                         if(reader.NodeType != XmlNodeType.EndElement)
                         {
                             throw new Exception($"Expected </notification> closing element {reader.NodeType}: {reader.Name}");
                         }
                         return value;
                     }
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction(MakeName(Argument)))}}
                 }
                 """;
    }

    private string GetXmlWriting()
    {
        if (Parent is Module)
        {
            return "await WriteXMLAsync(writer);";
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

        if (parent is IXMLSource)
        {
            return "await WriteXMLAsync(writer);";
        }

        throw new SemanticError(
            $"Top level statement of 'notification {Argument}' ({parent?.Source.Keyword} {parent?.Argument}) was not a valid XML source",
            Source);
    }

    private string GetXmlReading()
    {
        if (Parent is Module)
        {
            return "await ParseAsync(reader);";
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

        if (parent is IXMLSource)
        {
            return "await ParseAsync(reader);";
        }

        throw new SemanticError(
            $"Top level statement of 'notification {Argument}' ({parent?.Source.Keyword} {parent?.Argument}) was not a valid XML source",
            Source);
    }
}