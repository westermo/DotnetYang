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

    public string ServerDeclaration => ReturnType + " On" + MakeName(Argument) + "(" +
                                       (Ingoing is null ? string.Empty : InputType + " input") + ");";

    private string OutputType => MakeName(Argument) + "Output";

    private string ReturnType => Outgoing is null
        ? "Task"
        : "Task<" + MakeNamespace(this.GetModule()!.Argument) + ".YangNode." + OutputType + ">";

    private string InputType =>
        MakeNamespace(this.GetModule()!.Argument) + ".YangNode." + MakeName(Argument) + "Input";

    public string ReceiveCase => $"case \"{Argument}\" when reader.NamespaceURI is \"{Namespace}\":\n" +
                                 (Ingoing is null
                                     ? $$"""
                                         {
                                             var task = server.On{{MakeName(Argument)}}();
                                         """
                                     : $$"""
                                         {
                                             var input = await {{InputType}}.ParseAsync(reader);
                                             var task = server.On{{MakeName(Argument)}}(input);
                                         """)
                                 + "\n    " + RpcCodeSnippets.WriteServerResponse(Outgoing is not null).Replace("\n", "\n    ")
                                 + "\n}\nbreak;";

    public override string ToCode()
    {
        StringBuilder builder = new();
        builder.AppendLine(DescriptionString);
        builder.AppendLine(AttributeString);
        var inputType = Ingoing is null ? string.Empty : ", " + InputType + " input";
        builder.AppendLine(
            $"public static async {ReturnType} {MakeName(Argument)}(IChannel channel, int messageID{inputType})");
        builder.AppendLine($$"""
                             {
                                 {{Indent(RpcCodeSnippets.OpenRpcEnvelope)}}
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
                               await channel.Send();
                           """);
        builder.AppendLine("\t" + Indent(RpcCodeSnippets.ReadReply(ReturnType != "Task" ? OutputType : null)));

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