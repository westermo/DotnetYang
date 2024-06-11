using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Action : NodeDataStatement, IXMLParseable
{
    public Action(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        Ingoing = Children.FirstOrDefault(x => x is Input) as Input;
        Outgoing = Children.FirstOrDefault(x => x is Output) as Output;
    }

    private Input? Ingoing { get; }

    private Output? Outgoing { get; }

    public const string Keyword = "action";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Input.Keyword),
        new ChildRule(Output.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
    ];

    public string ServerDeclaration => ReturnType + " On" + MakeName(Argument) + $"({QualifiedRootName} root, " +
                                       (Ingoing is null
                                           ? FullyQualifiedNamespace() + " target"
                                           : FullyQualifiedNamespace() + "." + InputType + " input") + ");";

    private string? _outputType;
    private string OutputType => _outputType ??= MakeName(Argument) + "Output";
    private string? _returnType;

    private string ReturnType => _returnType ??=
        Outgoing is null ? "Task" : "Task<" + FullyQualifiedNamespace() + "." + OutputType + ">";

    private string? _inputType;
    private string InputType => _inputType ??= MakeName(Argument) + "Input";

    public override string ToCode()
    {
        var inputType = Ingoing is null ? string.Empty : ", " + InputType + " input";
        var inputCall = Ingoing is null ? string.Empty : "Input = input";
        var inputField = Ingoing is null ? string.Empty : "public " + InputType + "? Input;";
        var writeFunction = Ingoing is null
            ? $$"""
                public async Task WriteXMLAsync(XmlWriter writer)
                {
                    await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                    await writer.WriteEndElementAsync();
                }
                """
            : $$"""
                public async Task WriteXMLAsync(XmlWriter writer)
                {
                    await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                    await Input!.WriteXMLAsync(writer);
                    await writer.WriteEndElementAsync();
                }
                """;
        var readFunction = Ingoing is null
                ? $$"""
                    public static async Task<{{ClassName}}> ParseAsync(XmlReader reader)
                    {
                        while(await reader.ReadAsync())
                        {
                           switch(reader.NodeType)
                           {
                               case XmlNodeType.Element:
                                    throw new Exception($"Unexpected element '{reader.Name}' under '{{XmlObjectName}}'");
                               case XmlNodeType.EndElement when reader.Name == "{{XmlObjectName}}":
                                   return new {{ClassName}}();
                               case XmlNodeType.Whitespace: break;
                               default: throw new Exception($"Unexpected node type '{reader.NodeType}' : '{reader.Name}' under '{{XmlObjectName}}'");
                           }
                        }
                        throw new Exception("Reached end-of-readability without ever returning from {{ClassName}}.ParseAsync");
                    }
                    """
                : $$"""
                    public static async Task<{{ClassName}}> ParseAsync(XmlReader reader)
                    {
                        {{InputType}} input = await {{InputType}}.ParseAsync(reader);
                        if(reader.NodeType != XmlNodeType.EndElement || reader.Name != "{{XmlObjectName}}")
                        {
                            throw new Exception($"Unexpected node type '{reader.NodeType}' : '{reader.Name}' under '{{XmlObjectName}}'");
                        }
                        return new {{ClassName}}{
                            Input = input
                        };
                    }
                    """
            ;
        var returnFunction = Outgoing is not null
            ? $$"""
                using XmlReader reader = XmlReader.Create(response,SerializationHelper.GetStandardReaderSettings());
                await reader.ReadAsync();
                if(reader.NodeType != XmlNodeType.Element || reader.Name != "rpc-reply" || reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0" || reader["message-id"] != messageID.ToString())
                {
                    throw new Exception($"Expected stream to start with a <rpc-reply> element with message id {messageID} & \"urn:ietf:params:xml:ns:netconf:base:1.0\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
                }
                var value = await {{OutputType}}.ParseAsync(reader);
                response.Dispose();
                return value;
                """
            : """
              using XmlReader reader = XmlReader.Create(response,SerializationHelper.GetStandardReaderSettings());
              await SerializationHelper.ExpectOkRpcReply(reader, messageID);
              response.Dispose();
              """;
        var call = $$"""
                     public async {{ReturnType}} {{MakeName(Argument)}}(IChannel channel, int messageID, {{QualifiedRootName}} root{{inputType}})
                     {
                         {{TargetName}} = new {{ClassName}}
                         {
                             {{inputCall}}
                         };
                         StringBuilder stringBuilder = new StringBuilder();
                         using XmlWriter writer = XmlWriter.Create(stringBuilder, SerializationHelper.GetStandardWriterSettings());
                         await writer.WriteStartElementAsync(null,"rpc","urn:ietf:params:xml:ns:netconf:base:1.0");
                         await writer.WriteAttributeStringAsync(null,"message-id",null,messageID.ToString());
                         await writer.WriteStartElementAsync(null,"action","urn:ietf:params:xml:ns:yang:1");
                         await root.WriteXMLAsync(writer);
                         await writer.WriteEndElementAsync();
                         await writer.WriteEndElementAsync();
                         await writer.FlushAsync();
                         {{TargetName}} = null;
                         var response = await channel.Send(stringBuilder.ToString());
                         {{Indent(returnFunction)}}
                     }
                     """;
        return $$"""
                 public {{ClassName}}? {{TargetName}} { get; private set; }
                 {{DescriptionString}}{{AttributeString}}
                 public class {{ClassName}}
                 {
                     {{inputField}}
                     {{Indent(writeFunction)}}
                     {{Indent(readFunction)}}
                 }
                 {{Outgoing?.ToCode()}}
                 {{Ingoing?.ToCode()}}
                 {{call}}
                 """;
    }

    protected override void ValidateParent()
    {
        var parent = Parent;
        while (parent is not null)
        {
            if (parent is Rpc or Action or Notification)
            {
                throw new SemanticError($"Action may not exist inside {parent.GetType().Name} block", Source);
            }

            parent = parent.Parent;
        }
    }
    public string ReceiveCase =>
                                 (Ingoing is null
                                     ? $$"""
                                         if({{TargetPath}} != null) {
                                             var task = server.On{{MakeName(Argument)}}({{Root.TargetName}}, {{TargetPath.Replace("?." + TargetName, "")}}!);
                                         """
                                     : $$"""
                                         if({{TargetPath}} != null) {
                                             var task = server.On{{MakeName(Argument)}}({{Root.TargetName}}, {{TargetPath}}?.Input!);
                                         """)
                                 + "\n" + (Outgoing is null
                                     ? """
                                           await task;
                                           await writer.WriteStartElementAsync(null,"ok","urn:ietf:params:xml:ns:netconf:base:1.0");
                                           await writer.WriteEndElementAsync();
                                           return;
                                       }
                                       """
                                     : """
                                           var response = await task;
                                           await response.WriteXMLAsync(writer);
                                           return;
                                       }
                                       """);

    private string? _target;

    public override string TargetName => _target ??= MakeName(Argument) + "ActionNode";

    public string WriteCall =>
        Ingoing is not null
            ? $$"""
                if({{MakeName(Argument)}}InputValue is not null)
                {
                    await writer.WriteStartElementAsync({{xmlPrefix}},"{{Source.Argument}}",{{xmlNs}});
                    await {{MakeName(Argument)}}InputValue.WriteXMLAsync(writer);
                    await writer.WriteEndElementAsync();
                }
                """
            : $$"""
                if({{MakeName(Argument)}}Active)
                {
                    await writer.WriteStartElementAsync(null,"{{Source.Argument}}",null);
                    await writer.WriteEndElementAsync();
                }
                """;

    public string ParseCall =>
        Ingoing is not null
            ? $"""
               await {MakeName(Argument)}Input.ParseAsync(reader);
               await reader.ReadAsync();//Parse it out, but ignore
               """
            : "await reader.ReadAsync();";

    private string? _className;
    public override string ClassName => _className ??= MakeName(Argument) + "Class";
}