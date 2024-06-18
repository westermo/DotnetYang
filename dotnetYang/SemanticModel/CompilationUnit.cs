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
        Dictionary<string, List<string>> ActionCases = [];
        Dictionary<string, List<string>> NotificationCases = [];
        foreach (var module in Children.OfType<Module>())
        {
            foreach (var action in module.Actions)
            {
                var caseName = $$"""
                                 case "{{action.Root.XmlObjectName}}" when reader.NamespaceURI is "{{action.Root.Namespace}}":
                                 {
                                     var {{action.Root.TargetName}} = await {{action.QualifiedRootName}}.ParseAsync(reader);
                                     
                                 """;
                if (!ActionCases.TryGetValue(caseName, out var list))
                {
                    ActionCases[caseName] = [];
                    list = ActionCases[caseName];
                }

                list.Add(action.ReceiveCase);
            }

            foreach (var notification in module.Notifications.Where(n => n.IsTopLevel == false))
            {
                var caseName = $$"""
                                 case "{{notification.Root.XmlObjectName}}" when reader.NamespaceURI is "{{notification.Root.Namespace}}":
                                 {
                                     var {{notification.Root.TargetName}} = await {{notification.QualifiedRootName}}.ParseAsync(reader);
                                     
                                 """;
                if (!NotificationCases.TryGetValue(caseName, out var list))
                {
                    NotificationCases[caseName] = [];
                    list = NotificationCases[caseName];
                }

                list.Add(notification.ReceiveCase);
            }
        }

        Dictionary<string, string> a = new()
        {
            ["a"] = "b"
        };

        return $$"""
                 using System;
                 using System.Xml;
                 using YangSupport;
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
                        var initialPosition = output.Position;
                        var initialLength = output.Length;
                        string? id = null;
                        using XmlReader reader = XmlReader.Create(input, SerializationHelper.GetStandardReaderSettings());
                        using XmlWriter writer = XmlWriter.Create(output, SerializationHelper.GetStandardWriterSettings());
                        try
                        {
                            await reader.ReadAsync();
                            switch(reader.Name)
                            {
                                case "rpc":
                                    id = reader.ParseMessageId();
                                    await writer.WriteStartElementAsync(null, "rpc-reply", "urn:ietf:params:xml:ns:netconf:base:1.0");
                                    await writer.WriteAttributeStringAsync(null, "message-id", null, id);
                                    await reader.ReadAsync();
                                    switch(reader.Name)
                                    {
                                        case "action":
                                            await server.ReceiveAction(reader, writer);
                                            break;
                                        default:
                                            await server.ReceiveRPC(reader, writer);
                                            break;
                                    }
                                    await writer.WriteEndElementAsync();
                                    await writer.FlushAsync();
                                    break;
                                case "notification":
                                    var eventTime = await reader.ParseEventTime();
                                    await reader.ReadAsync();
                                    await server.ReceiveNotification(reader, eventTime);
                                    break;
                            }
                        }
                        catch(RpcException ex)
                        {
                            await writer.FlushAsync();
                            output.Position = initialPosition;
                            output.SetLength(initialLength);
                            await ex.SerializeAsync(output,id);
                        }
                        catch(Exception ex)
                        {
                            await writer.FlushAsync();
                            output.Position = initialPosition;
                            output.SetLength(initialLength);
                            await output.SerializeRegularExceptionAsync(ex,id);
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
                        await reader.ReadAsync();
                        switch(reader.Name)
                        {
                            {{Indent(Indent(Indent(string.Join("\n", ActionCases.Select(c => c.Key + Indent(string.Join("\n", c.Value)) + "\n}\nthrow new Exception(\"Could not find valid action\");")))))}}
                        }
                    }
                    public static async Task ReceiveNotification(this IYangServer server, XmlReader reader, DateTime eventTime)
                    {
                        switch(reader.Name)
                        {
                            {{Indent(Indent(Indent(string.Join("\n", Children.OfType<Module>().SelectMany(m => m.Notifications.Where(n => n.IsTopLevel)).Select(rpc => rpc.ReceiveCase).Distinct()))))}}
                            {{Indent(Indent(Indent(string.Join("\n", NotificationCases.Select(c => c.Key + Indent(string.Join("\n", c.Value)) + "\n}\nthrow new Exception(\"Could not find valid notification\");")))))}}
                        }
                    }
                 }
                 """;
    }

    public string? TargetName => null;
    public string ClassName => "Configuration";
}