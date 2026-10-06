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
            members.Add($$"""
                          private {{typeName}}? _{{memberName}};
                          public {{typeName}}? {{memberName}}
                          {
                              get => _{{memberName}};
                              set
                              {
                                  if (_{{memberName}} is not null) _{{memberName}}.YangParent = null;
                                  _{{memberName}} = value;
                                  if (value is not null) value.YangParent = this;
                              }
                          }
                          """);
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

        // Generate GetChild for Configuration (maps module names to module properties)
        var configGetChildCases = new List<string>();
        foreach (var module in Children.OfType<Module>())
        {
            var memberName = MakeName(module.Argument);
            configGetChildCases.Add($"\"{module.Argument}\" => {memberName},");
        }
        var configGetChild = configGetChildCases.Count > 0
            ? $$"""
                 public object? GetChild(string yangName) => yangName switch
                 {
                     {{Indent(string.Join("\n", configGetChildCases))}}
                     _ => null
                 };
                 """
            : "public object? GetChild(string yangName) => null;";

        var moduleValidations = Children.OfType<Module>()
            .Select(module => $"ValidateModule({MakeName(module.Argument)}, failures);");
        var configValidate = $$"""
                               /// <summary>
                               /// Validates every module subtree present in this configuration.
                               /// Throws <see cref="global::YangSupport.YangValidationException"/> for a single violation or
                               /// <see cref="global::YangSupport.YangValidationAggregateException"/> when several modules fail.
                               /// </summary>
                               public void YangValidate()
                               {
                                   var failures = new global::System.Collections.Generic.List<global::YangSupport.YangValidationException>();
                                   {{Indent(string.Join("\n", moduleValidations))}}
                                   if (failures.Count == 1) throw failures[0];
                                   if (failures.Count > 1) throw new global::YangSupport.YangValidationAggregateException(failures);
                               }

                               private static void ValidateModule(global::YangSupport.IYangValidatable? module, global::System.Collections.Generic.List<global::YangSupport.YangValidationException> failures)
                               {
                                   if (module is null) return;
                                   try
                                   {
                                       module.YangValidate();
                                   }
                                   catch (global::YangSupport.YangValidationException ex)
                                   {
                                       failures.Add(ex);
                                   }
                                   catch (global::YangSupport.YangValidationAggregateException ex)
                                   {
                                       failures.AddRange(ex.Failures);
                                   }
                               }
                               """;

        return $$"""
                 using System;
                 using System.Xml;
                 using YangSupport;
                 namespace {{MyNamespace}};
                 ///<summary>
                 ///Configuration root object for {{MyNamespace}} based on provided .yang modules
                 ///</summary>{{AttributeString}}
                 public class Configuration : YangSupport.IYangNode, global::YangSupport.IYangValidatable, global::YangSupport.IYangInstanceIdentifierRoot
                 {
                     YangSupport.IYangNode? YangSupport.IYangNode.YangParent => null;
                     {{Indent(string.Join("\n", members))}}
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                     {{Indent(configGetChild)}}
                     {{Indent(configValidate)}}
                     /// <summary>
                     /// Resolves an instance-identifier path to the target object in the data tree.
                     /// Path format: /module-name:container/child/list[key='value']/leaf
                     /// </summary>
                     public object? ResolveInstanceIdentifier(string path) => global::YangSupport.InstanceIdentifierResolver.Resolve(this, path);
                 }
                 {{ServerExtensions(ActionCases, NotificationCases)}}
                 """;
    }

    private string ServerExtensions(Dictionary<string, List<string>> ActionCases, Dictionary<string, List<string>> NotificationCases)
    {
        var hasRpcs = Children.OfType<Module>().Any(m => m.Rpcs.Count > 0);
        var hasActions = Children.OfType<Module>().Any(m => m.Actions.Count > 0);
        var hasNotifications = Children.OfType<Module>().Any(m => m.Notifications.Count > 0);

        if (!hasRpcs && !hasActions && !hasNotifications)
            return string.Empty;

        return $$"""
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