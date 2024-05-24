using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Enumeration() : BuiltinType("enumeration", (s) =>
{
    var name = Statement.MakeName(s.Parent!.Argument);
    var enums = s.Children.OfType<Enum>().ToArray();
    var others = s.Children.Except(enums);
    var strings = enums.Select(e => e.ToCode());
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