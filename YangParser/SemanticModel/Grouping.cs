using System;
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
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore)
    ];

    public override string ToCode()
    {
        return string.Empty;
    }

    public IStatement[] WithUse(Uses use)
    {
        foreach (var child in this.Unwrap())
        {
            if (child is Uses inner)
            {
                inner.Expand();
            }

            if (child is not Type type) continue;
            if (type.Argument.Contains("identityref"))
            {
                foreach (var baseType in type.Children.OfType<Base>())
                {
                    if (baseType.Argument.Contains(':') ||
                        baseType.Argument.Contains('.')) //Is prefixed already, leave be
                    {
                        continue;
                    }

                    baseType.Argument = this.GetInheritedPrefix() + ":" + baseType.Argument;
                }

                continue;
            }

            if (BuiltinTypeReference.IsBuiltin(type, out _, out _))
            {
                continue;
            }

            if (type.Argument.Contains(':') || type.Argument.Contains('.')) //Is prefixed already, leave be
            {
                continue;
            }

            type.Argument = this.GetInheritedPrefix() + ":" + type.Argument;
        }

        //Propogate usings upwards
        if (use.GetModule() is Module target)
        {
            if (this.GetModule() is Module source)
            {
                if (source != target)
                {
                    foreach (var pair in source.Usings)
                    {
                        if (!target.Usings.ContainsKey(pair.Key))
                        {
                            // Log.Write($"Adding prefix {pair.Key} to '{target.Argument}' from '{source.Argument}'");
                            target.Usings[pair.Key] = pair.Value;
                        }
                    }
                }
            }
        }

        var containingModule = this.GetModule();
        if (containingModule is null)
        {
            Log.Write($"Error, could not find containing module for grouping '{Argument}'");
        }
        else
        {
            containingModule.Expand();
        }


        return Children;
    }
}