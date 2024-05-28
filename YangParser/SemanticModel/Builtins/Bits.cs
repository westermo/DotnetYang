using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Bits() : BuiltinType("bits", statement =>
{
    var name = BuiltinTypeReference.TypeName(statement);
    var bits = statement.Children.OfType<Bit>().ToArray();
    var strings = bits.Select(e => e.ToCode());
    foreach (var child in statement.Children)
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