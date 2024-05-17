using System;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public static class StatementFactory
{
    public static IStatement Create(YangStatement source)
    {
        return source.Keyword switch
        {
            Module.Keyword => new Module(source),
            Leaf.Keyword => new Leaf(source),
            Container.Keyword => new Container(source),
            LeafList.Keyword => new LeafList(source),
            Notification.Keyword => new Notification(source),
            Choice.Keyword => new Choice(source),
            Rpc.Keyword => new Rpc(source),
            Augment.Keyword => new Augment(source),
            When.Keyword => new When(source),
            Case.Keyword => new Case(source),
            Grouping.Keyword => new Grouping(source),
            TypeDefinition.Keyword => new TypeDefinition(source),
            List.Keyword => new List(source),
            Import.Keyword => new Import(source),
            Pattern.Keyword => new Pattern(source),
            Uses.Keyword => new Uses(source),
            Extension.Keyword => new Extension(source),
            Deviation.Keyword => new Deviation(source),
            Identity.Keyword => new Identity(source),
            Include.Keyword => new Include(source),
            Organization.Keyword => new Organization(source),
            Prefix.Keyword => new Prefix(source),
            Revision.Keyword => new Revision(source),
            Namespace.Keyword => new Namespace(source),
            Contact.Keyword => new Contact(source),
            Description.Keyword => new Description(source),
            AnyXml.Keyword => new AnyXml(source),
            Status.Keyword => new Status(source),
            DefaultValue.Keyword => new DefaultValue(source),
            Mandatory.Keyword => new Mandatory(source),
            StateData.Keyword => new StateData(source),
            Reference.Keyword => new Reference(source),
            Units.Keyword => new Units(source),
            Must.Keyword => new Must(source),
            Action.Keyword => new Action(source),
            Type.Keyword => new Type(source),
            Enum.Keyword => new Enum(source),
            Bit.Keyword => new Bit(source),
            Value.Keyword => new Value(source),
            Length.Keyword => new Length(source),
            Path.Keyword => new Path(source),
            RequireInstance.Keyword => new RequireInstance(source),
            Key.Keyword => new Key(source),
            Unique.Keyword => new Unique(source),
            OrderedBy.Keyword => new OrderedBy(source),
            MaxElements.Keyword => new MaxElements(source),
            MinElements.Keyword => new MinElements(source),
            FeatureFlag.Keyword => new FeatureFlag(source),
            Input.Keyword => new Input(source),
            Output.Keyword => new Output(source),
            Presence.Keyword => new Presence(source),
            _ => throw new InvalidOperationException($"Unknown keyword {source.Keyword}")
        };
    }
}