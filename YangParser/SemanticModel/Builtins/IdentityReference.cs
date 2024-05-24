using System.Linq;
using YangParser.Generator;

namespace YangParser.SemanticModel.Builtins;

public class IdentityReference() : BuiltinType("identityref", s =>
{
    var inherits = s.Children.OfType<Base>().Select(Statement.InterfaceName).ToArray();
    if (inherits.Length == 1)
    {
        var value = inherits[0];
        return (value, null);
    }

    var inheritance = inherits.Length == 0 ? string.Empty : " : " + string.Join(", ", inherits);
    var name = Statement.MakeName(s.Parent!.Argument);
    var definition = $"""
                      {s.DescriptionString}{s.AttributeString}
                      public class {name}{inheritance};
                      """;
    return (name, definition);
});