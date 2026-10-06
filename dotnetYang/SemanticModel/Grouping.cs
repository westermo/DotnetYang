using System.Linq;
using YangParser.Generator;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class Grouping : Statement
{
    public Grouping(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "grouping";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Action.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        return string.Empty;
    }

    public IStatement[] WithUse(Uses use)
    {
        var copy = StatementFactory.Create(Source);
        Parent!.Insert([copy]);
        copy.Parent = Parent;
        StatementExtensions.PrepareForRelocation(copy, use, copy, expandUses: true);

        //Propagate usings upwards
        if (use.GetModule() is Module target && copy.GetModule() is Module source)
        {
            StatementExtensions.PropagateImports(source, target, includePrefixTable: false);
        }

        var containingModule = copy.GetModule();
        if (containingModule is null)
        {
            Log.Write($"Error, could not find containing module for grouping '{Argument}'");
        }
        else
        {
            containingModule.Expand();
        }

        Parent.Replace(copy, []);
        foreach (var refinement in use.Children.OfType<Refine>())
        {
            var path = refinement.Argument.Split('/');
            var current = copy;
            foreach (var element in path)
            {
                element.Prefix(out var name);
                var origin = current;
                current = StatementExtensions.LocateChildWithInvisibleAllowed(current.Children, name);
                if (current is null)
                {
                    Log.Write(
                        $"Missing '{name}' in '{refinement.Argument}' at '{origin.Source.Keyword} {origin.Argument}' [{string.Join(", ", origin.Children.Select(o => o.GetType().Name + " " + o.Argument))}]");
                    break; //Target not present, nothing to refine.
                }
            }

            current?.ApplyRefinement(refinement);
        }

        return copy.Children;
    }
}