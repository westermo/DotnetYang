using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class CompilationUnit : Statement, IXMLParseable
{
    public CompilationUnit(Module[] modules, string Namespace = "Somewhere") : base(new YangStatement(String.Empty,
        string.Empty, [],
        new Metadata(string.Empty, new Parser.Position(), 0)))
    {
        this.MyNamespace = Namespace;
        Children = modules;
    }

    public string MyNamespace { get; set; }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Module.Keyword, Cardinality.ZeroOrMore),
    ];

    public override string ToCode()
    {
        var members = new List<string>();
        foreach (var module in Children.OfType<Module>())
        {
            var typeName = module.MyNamespace.Substring(0, module.MyNamespace.Length - 1);
            var memberName = MakeName(module.Argument);
            members.Add($"public {typeName}? {memberName} {{ get; set; }}");
        }

        Argument = "root";

        return $$"""
                 using System;
                 using System.Xml;
                 using Yang.Attributes;
                 namespace {{MyNamespace}};
                 ///<summary>
                 ///Configuration root object for {{MyNamespace}} based on provided .yang modules
                 ///</summary>{{AttributeString}}
                 public class Configuration
                 {
                     {{Indent(string.Join("\n", members))}}
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                 }
                 public static class IYangServerExtensions
                 {
                    public static async Task Receive(this IYangServer server, global::System.IO.Stream input, global::System.IO.Stream output)
                    {
                        using XmlReader reader = XmlReader.Create(input, SerializationHelper.GetStandardReaderSettings());
                        using XmlWriter writer = XmlWriter.Create(output, SerializationHelper.GetStandardWriterSettings());
                        await reader.ReadAsync();
                        switch(reader.Name)
                        {
                            case "rpc":
                                if(reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0")
                                {
                                    throw new Exception($"Got an <rpc> element with namespace {reader.NamespaceURI}, expected \"urn:ietf:params:xml:ns:netconf:base:1.0\"");
                                }
                                var id = reader["message-id"];
                                await reader.ReadAsync();
                                await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
                                await writer.WriteAttributeStringAsync(null, "message-id", null, id);
                                switch(reader.Name)
                                {
                                    case "action":
                                        server.ReceiveAction(reader, writer);
                                        break;
                                    default:
                                        server.ReceiveRPC(reader, writer);
                                        break;
                                }
                                await writer.WriteEndElementAsync();
                                await writer.FlushAsync();
                                break;
                            case "notification":
                                if(reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:notification:1.0")
                                {
                                    throw new Exception($"Got an <rpc> element with namespace {reader.NamespaceURI}, expected \"urn:ietf:params:xml:ns:netconf:notification:1.0\"");
                                }
                                await reader.ReadAsync();
                                if(reader.Name != "eventTime")
                                {
                                    throw new Exception($"Expected an <eventTime> element as the first child of the <notification> element, but got {reader.Name}");
                                }
                                await reader.ReadAsync();
                                if(!global::System.DateTime.TryParse(await reader.GetValueAsync(), out var eventTime)) throw new Exception($"Expected <eventTime> element to contain a valid dateTime but got {reader.NodeType}: {reader.Name}");
                                await reader.ReadAsync();
                                if(reader.NodeType != XmlNodeType.EndElement)
                                {
                                    throw new Exception($"Expected <eventTime> element to only have one child but got {reader.NodeType}: {reader.Name}");
                                }
                                await reader.ReadAsync();
                                server.ReceiveNotification(reader, eventTime);
                                break;
                        }
                    }
                    public static async Task ReceiveRPC(this IYangServer server, XmlReader reader, XmlWriter writer)
                    {
                        switch(reader.Name)
                        {
                            {{Indent(Indent(Indent(string.Join("\n", Children.OfType<Module>().SelectMany(m => m.Rpcs).Select(rpc => rpc.ReceiveCase).Distinct()))))}}
                        }
                    }
                    public static async Task ReceiveAction(this IYangServer server, XmlReader reader, XmlWriter writer)
                    {
                        throw new NotImplementedException();
                    }
                    public static async Task ReceiveNotification(this IYangServer server, XmlReader reader, DateTime eventTime)
                    {
                        switch(reader.Name)
                        {
                            {{Indent(Indent(Indent(string.Join("\n", Children.OfType<Module>().SelectMany(m => m.Notifications).Select(rpc => rpc.ReceiveCase).Distinct()))))}}
                        }
                    }
                 }
                 """;
    }

    public string? TargetName => null;
    public string ClassName => "Configuration";
}