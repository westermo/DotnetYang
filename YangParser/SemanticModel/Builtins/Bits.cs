using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Bits() : BuiltinType("bits", s =>
{
    var name = Statement.MakeName(s.Parent!.Argument);
    var bits = s.Children.OfType<Bit>().ToArray();
    var others = s.Children.Except(bits);
    var strings = bits.Select(e => e.ToCode());
    foreach (var child in others)
    {
        child.ToCode();
    }

    var definition = $$"""
                       {{s.DescriptionString}}{{s.AttributeString}}
                       public enum {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", strings))}}
                       }
                       """;
    return (name, definition);
});