using System.Linq;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class Augment : Statement, IUnexpandable
{
    public Augment(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    private bool Expanded;

    public const string Keyword = "augment";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Case.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Refine.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Deviation.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Rpc.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Extension.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Identity.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(List.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore)
    ];

    public void Inject()
    {
        if (Expanded) return;
        var sourceNS = this.GetInheritedPrefix();
        this.GetModule()?.Expand();
        var components = Argument.Split('/');
        foreach (var child in Children.ToArray())
        {
            if (child is When when)
            {
                Replace(when, []);
                foreach (var other in Children)
                {
                    other.Insert([when]);
                }
            }

            if (child is FeatureFlag flag)
            {
                Replace(flag, []);
                foreach (var other in Children)
                {
                    other.Insert([flag]);
                }
            }
        }

        var top = Argument.StartsWith("/") ? GetModule(components) : Parent!;
        
        Parent?.Replace(this,[]);

        var target = GetTarget(top, components, sourceNS);

        target.Insert(Children);
        Expanded = true;
    }

    private IStatement GetModule(string[] components)
    {
        IStatement top;
        var first = components.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        if (first is null) throw new SemanticError($"Could not find augment path from {Argument}", Source);
        var prefix = first.Prefix(out _);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            top = this.GetModule() ??
                  throw new SemanticError($"Could not find module for Augment '{Argument}'", Source);
        }
        else
        {
            top = this.FindSourceFor(prefix) ??
                  throw new SemanticError($"Could not find module for prefix '{prefix}'", Source);
            //prefix children where necessary
            PrepareTransitionTo(top);
        }

        return top;
    }

    private IStatement GetTarget(IStatement top, string[] components, string sourceNS)
    {
        var current = top;
        foreach (var xpath in components)
        {
            if (string.IsNullOrWhiteSpace(xpath)) continue;
            xpath.Prefix(out var childName);
            if (childName == current.Argument) break;
            var origin = current;
            foreach (var currentChild in current.Children)
            {
                _ = currentChild.Argument.Prefix(out var trueName);
                if (currentChild is Prefix) continue;
                if (childName != trueName) continue;
                current = currentChild;
                break;
            }

            if (current == origin)
            {
                var newChild = new Container(childName, Source.Metadata);
                newChild.Attributes.Add($"Augmented(\"{sourceNS}\")");
                current.Insert([newChild]);
                current = newChild;
            }
        }

        switch (current)
        {
            case Container:
            case List:
            case Choice:
            case Case:
            case Input:
            case Output:
            case Notification:
                break;
            default:
                throw new SemanticError($"Augment target '{Argument}' was disallowed type " +
                                        $"'{current.GetType().Name} {current.Argument}'", Source);
        }

        return current;
    }

    private void PrepareTransitionTo(IStatement top)
    {
        foreach (var child in this.Unwrap())
        {
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

        //Propagate usings upwards
        if (this.GetModule() is Module source)
        {
            if (top is Module target)
            {
                if (source != top)
                {
                    foreach (var pair in source.Usings)
                    {
                        if (!target.Usings.ContainsKey(pair.Key))
                        {
                            target.Usings[pair.Key] = pair.Value;
                        }
                    }

                    foreach (var pair in source.ImportedModules)
                    {
                        if (!target.ImportedModules.ContainsKey(pair.Key))
                        {
                            target.ImportedModules[pair.Key] = pair.Value;
                        }
                    }
                }
            }
        }
    }

    public override string ToCode()
    {
        return string.Empty;
    }
}