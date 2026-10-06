using System.Collections.Generic;
using System.Linq;
using YangParser.Generator;

namespace YangParser.SemanticModel.Builtins;

public class IdentityReference() : BuiltinType("identityref", statement =>
{
    var bases = statement.Children.OfType<Base>().ToArray();
    if (bases.Length == 1)
    {
        var prefix = bases[0].Argument.Prefix(out _);
        if (!prefix.Contains('.') && !string.IsNullOrWhiteSpace(prefix) && !prefix.Contains(':'))
        {
            prefix += ":";
        }

        var source = statement.FindReference<Identity>(bases[0].Argument);
        if (source is not null)
        {
            var nname = source.ClassName;
            var sp = nname.Prefix(out _);
            if (sp.Contains('.') || sp.Contains(':'))
            {
                return (nname, null);
            }
            return (prefix + nname, null);
        }

        throw new SemanticError($"Could not locate source identity for {bases[0]}", bases[0].Source);
    }

    foreach (var child in statement.Children)
    {
        child.ToCode();
    }

    var first = bases[0];
    List<string> names = [Statement.MakeName(first.Argument)];
    var identity = statement.FindReference<Identity>(first.Argument) ??
                   throw new SemanticError($"Could not locate source identity for '{bases[0].Argument}'",
                       bases[0].Source);
    var set = Identity.GetInheritanceList(identity).ToArray();
    for (int i = 1; i < bases.Length; i++)
    {
        identity = statement.FindReference<Identity>(bases[i].Argument) ??
                   throw new SemanticError($"Could not locate source identity for '{bases[i].Argument}'",
                       bases[i].Source);
        set = set.Intersect(Identity.GetInheritanceList(identity)).ToArray();
        names.Add(Statement.MakeName(bases[i].Argument));
    }

    var className = BuiltinTypeReference.TypeName(statement);
    var definition = IdentityEnumEmitter.Emit(className, set,
        statement.DescriptionString + statement.AttributeString, includeNullableOverloads: false);
    return (className, definition);
});