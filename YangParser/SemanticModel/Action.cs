using System;
using System.Linq;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Action : Statement, IXMLValue
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
        //TODO: REWORK
        StringBuilder builder = new();
        builder.AppendLine(DescriptionString);
        builder.AppendLine(AttributeString);
        var outputType = MakeName(Argument) + "Output";
        var returnType = Outgoing is null ? "Task" : "Task<" + outputType + ">";
        var inputType = Ingoing is null ? string.Empty : ", " + MakeName(Argument) + "Input input";
        builder.AppendLine(
            $"public async {returnType} {MakeName(Argument)}(IChannel channel, int messageID{inputType})");
        builder.AppendLine("""
                           {
                               XmlWriterSettings settings = new XmlWriterSettings();
                               settings.Indent = true;
                               settings.OmitXmlDeclaration = true;
                               settings.NewLineOnAttributes = true;
                               settings.Async = true;
                               StringBuilder stringBuilder = new StringBuilder();
                               using XmlWriter writer = XmlWriter.Create(stringBuilder, settings);
                               await writer.WriteStartElementAsync(null,"rpc","urn:ietf:params:xml:ns:netconf:base:1.0");
                               await writer.WriteAttributeStringAsync(null,"message-id",null,messageID.ToString());
                               await writer.WriteStartElementAsync(null,"action","urn:ietf:params:xml:ns:yang:1");
                           """);
        if (Ingoing is not null)
        {
            builder.AppendLine($"\tthis.{MakeName(Argument)}InputValue = input;");
        }
        else
        {
            builder.AppendLine($"\tthis.{MakeName(Argument)}Active = true;");
        }

        builder.AppendLine("""
                               await WriteXML(writer);
                               await writer.WriteEndElementAsync();
                               await writer.WriteEndElementAsync();
                               var xml = stringBuilder.ToString();
                           """);
        builder.AppendLine(returnType != "Task"
            ? "\tvar response = channel.Send(xml);"
            : "\tchannel.Send(xml);");
        if (Ingoing is not null)
        {
            builder.AppendLine($"\tthis.{MakeName(Argument)}InputValue = null;");
        }
        else
        {
            
            builder.AppendLine($"\tthis.{MakeName(Argument)}Active = false;");
        }

        if (returnType != "Task")
        {
            builder.AppendLine($"\treturn {outputType}.Parse(response);");
        }

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

    public string WriteCall
    {
        get
        {
            if (Ingoing is not null)
            {
                return $$"""
                         if({{MakeName(Argument)}}InputValue is not null)
                         {
                             await writer.WriteStartElementAsync(null,"{{Source.Argument}}",null);
                             await {{MakeName(Argument)}}InputValue.WriteXML(writer);
                             await writer.WriteEndElementAsync();
                         }
                         """;
            }

            return $$"""
                    if({{MakeName(Argument)}}Active)
                    {
                        await writer.WriteStartElementAsync(null,"{Source.Argument}",null);
                        await writer.WriteEndElementAsync();
                    }
                    """;
        }
    }
}