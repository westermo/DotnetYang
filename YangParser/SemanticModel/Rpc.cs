using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Rpc : Statement, IFunctionSource
{
    public List<string> Comments { get; } = new();

    public Rpc(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        ValidateChildren(statement);
        Flags = Children.OfType<FeatureFlag>().ToArray();
        Description = Children.FirstOrDefault(child => child is Description)?.Argument;
        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        Ingoing = Children.FirstOrDefault(x => x is Input) as Input;
        Outgoing = Children.FirstOrDefault(x => x is Output) as Output;
    }

    private Input? Ingoing { get; set; }

    private Output? Outgoing { get; set; }

    private FeatureFlag[] Flags { get; }
    private string? Description { get; }

    public const string Keyword = "rpc";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(SemanticModel.Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Input.Keyword),
        new ChildRule(Output.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
    ];

    public override string ToCode()
    {
        StringBuilder builder = new();
        builder.AppendLine(DescriptionString);
        builder.AppendLine(AttributeString);
        var outputType = MakeName(Argument) + "Output";
        var returnType = Outgoing is null ? "Task" : "Task<" + outputType + ">";
        var inputType = Ingoing is null ? string.Empty : ", " + MakeName(Argument) + "Input input";
        builder.AppendLine(
            $"public static async {returnType} {MakeName(Argument)}(IChannel channel, int messageID{inputType})");
        builder.AppendLine($$"""
                             {
                                 StringBuilder stringBuilder = new StringBuilder();
                                 using XmlWriter writer = XmlWriter.Create(stringBuilder, SerializationHelper.GetStandardWriterSettings());
                                 await writer.WriteStartElementAsync(null,"rpc","urn:ietf:params:xml:ns:netconf:base:1.0");
                                 await writer.WriteAttributeStringAsync(null,"message-id",null,messageID.ToString());
                                 await writer.WriteStartElementAsync("{{Prefix}}","{{Argument}}","{{Namespace}}");

                             """);
        if (inputType != string.Empty)
        {
            builder.AppendLine("\tawait input.WriteXMLAsync(writer);");
        }

        builder.AppendLine("""
                               await writer.WriteEndElementAsync();
                               await writer.WriteEndElementAsync();
                               await writer.FlushAsync();
                               var response = await channel.Send(stringBuilder.ToString());
                           """);
        builder.AppendLine(returnType != "Task"
            ? $$"""
                    using XmlReader reader = XmlReader.Create(response,SerializationHelper.GetStandardReaderSettings());
                    await reader.ReadAsync();
                    if(reader.NodeType != XmlNodeType.Element || reader.Name != "rpc-reply" || reader.NamespaceURI != "urn:ietf:params:xml:ns:netconf:base:1.0" || reader["message-id"] != messageID.ToString())
                    {
                        throw new Exception($"Expected stream to start with a <rpc-reply> element with message id {messageID} & \"urn:ietf:params:xml:ns:netconf:base:1.0\" but got {reader.NodeType}: {reader.Name} in {reader.NamespaceURI}");
                    }
                	var value = await {{outputType}}.ParseAsync(reader);
                    response.Dispose();
                    return value;
                """
            : """
                  using XmlReader reader = XmlReader.Create(response,SerializationHelper.GetStandardReaderSettings());
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
        }

        return builder.ToString();
    }
}