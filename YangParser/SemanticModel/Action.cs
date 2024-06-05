using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Action : Statement, IXMLWriteValue, IXMLAction
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

    public string ServerDeclaration => ReturnType + " On" + MakeName(Argument) + "(" +
                                       (Ingoing is null
                                           ? FullyQualifiedNamespace() + " target"
                                           : FullyQualifiedNamespace() + "." + InputType + " input") + ");";

    private string OutputType => MakeName(Argument) + "Output";

    private string ReturnType =>
        Outgoing is null ? "Task" : "Task<" + FullyQualifiedNamespace() + "." + OutputType + ">";

    private string InputType => MakeName(Argument) + "Input";

    public override string ToCode()
    {
        //TODO: REWORK
        StringBuilder builder = new();
        builder.AppendLine(DescriptionString);
        builder.AppendLine(AttributeString);
        var inputType = Ingoing is null ? string.Empty : ", " + InputType + " input";
        builder.AppendLine(
            $"public async {ReturnType} {MakeName(Argument)}(IChannel channel, int messageID{inputType})");
        builder.AppendLine("""
                           {
                               StringBuilder stringBuilder = new StringBuilder();
                               using XmlWriter writer = XmlWriter.Create(stringBuilder, SerializationHelper.GetStandardWriterSettings());
                               await writer.WriteStartElementAsync(null,"rpc","urn:ietf:params:xml:ns:netconf:base:1.0");
                               await writer.WriteAttributeStringAsync(null,"message-id",null,messageID.ToString());
                               await writer.WriteStartElementAsync(null,"action","urn:ietf:params:xml:ns:yang:1");
                           """);
        builder.AppendLine(Ingoing is not null
            ? $"""	this.{MakeName(Argument)}InputValue = input;"""
            : $"""	this.{MakeName(Argument)}Active = true;""");

        builder.AppendLine("""
                               await WriteXMLAsync(writer);
                               await writer.WriteEndElementAsync();
                               await writer.FlushAsync();
                               var response = await channel.Send(stringBuilder.ToString());
                           """);
        builder.AppendLine(Ingoing is not null
            ? $"\tthis.{MakeName(Argument)}InputValue = null;"
            : $"\tthis.{MakeName(Argument)}Active = false;");

        builder.AppendLine(ReturnType != "Task"
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
                  await reader.ReadAsync();
                  await SerializationHelper.ExpectOkRpcReply(reader, messageID);
                  response.Dispose();
              """);

        builder.AppendLine("}");
        if (Outgoing is not null)
        {
            builder.AppendLine(Outgoing.ToCode());
        }

        if (Ingoing is not null)
        {
            builder.AppendLine(Ingoing.ToCode());
            builder.AppendLine($"private {MakeName(Argument)}Input? {MakeName(Argument)}InputValue {{ get; set; }}");
        }
        else
        {
            builder.AppendLine($"private bool {MakeName(Argument)}Active {{ get; set; }}");
        }

        return builder.ToString();
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

    public string TargetName => MakeName(Argument);

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
                    await writer.WriteStartElementAsync(null,"{Source.Argument}",null);
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
}