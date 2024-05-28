using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Enumeration() : BuiltinType("enumeration", (statement) =>
{
    var name = BuiltinTypeReference.TypeName(statement);
    var enums = statement.Children.OfType<Enum>().ToArray();
    var others = statement.Children.Except(enums);
    var strings = enums.Select(e => e.ToCode());
    foreach (var child in others)
    {
        child.ToCode();
    }

    var definition = $$"""
                       {{statement.DescriptionString}}{{statement.AttributeString}}
                       public enum {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", strings))}}
                       }
                       """;
    return (name, definition);
});