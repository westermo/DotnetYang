using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Identity : Statement
{
    public Identity(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "identity";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Base.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore)
    ];

    public HashSet<Identity> Inheritors { get; } = new();

    public static IEnumerable<Identity> GetInheritanceList(Identity identity)
    {
        yield return identity;
        foreach (var inheritor in identity.Inheritors)
        {
            foreach (var recurse in GetInheritanceList(inheritor))
            {
                yield return recurse;
            }
        }
    }

    public string ClassName => MakeName(Argument) + "Identity";

    public void Expand()
    {
        foreach (var baseIdentityReference in Children.OfType<Base>())
        {
            var prefix = baseIdentityReference.Argument.Prefix(out var id);
            var source = this.FindSourceFor(prefix) as Module ?? this.GetModule() as Module;
            if (source is not null)
            {
                foreach (var identity in source.Identities.Where(i => i.Argument == id))
                {
                    if (identity.Inheritors.Add(this))
                    {
                    }
                }
            }
        }
    }

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        return IdentityEnumEmitter.Emit(ClassName, GetInheritanceList(this).ToArray(),
            DescriptionString + AttributeString, includeNullableOverloads: true);
    }
}