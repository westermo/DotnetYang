using System;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Action : Statement
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

    public override string ToCode()
    {
        StringBuilder builder = new();
        builder.AppendLine(DescriptionString);
        builder.AppendLine(AttributeString);
        var returnType = Outgoing is null ? "void" : MakeName(Argument) + "Output";
        var inputType = Ingoing is null ? string.Empty : ", " + MakeName(Argument) + "Input input";
        builder.AppendLine(
            $"public static {returnType} {MakeName(Argument)}(IChannel channel, int messageID{inputType})");
        builder.AppendLine("{");
        var ns = Parent is Module module ? "xmlns=\\\"" + module.XmlNamespace.Argument + "\\\"" : string.Empty;
        builder.AppendLine(inputType != string.Empty
            ? $$"""
                    var xml = $"<rpc message-id=\"{messageID}\" xmlns=\"urn:ietf:params:xml:ns:netconf:base:1.0\"><{{Argument}} {{ns}}>{input.ToXML()}</{{Argument}}></rpc>";
                """
            : $$"""
                    var xml = $"<rpc message-id=\"{messageID}\" xmlns=\"urn:ietf:params:xml:ns:netconf:base:1.0\"><{{Argument}} {{ns}}/></rpc>";
                """);

        builder.AppendLine(returnType != "void"
            ? "\tvar response = channel.Send(xml);"
            : "\tchannel.Send(xml);");
        if (returnType != "void")
        {
            builder.AppendLine($"\treturn {returnType}.Parse(response);");
        }

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
}