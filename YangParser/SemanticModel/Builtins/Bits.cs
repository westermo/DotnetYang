using System.Collections.Generic;
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

    var cases = new List<string>();
    foreach (var e in bits)
    {
        cases.Add($"if ((value & {name}.{Statement.MakeName(e.Argument)}) == {name}.{Statement.MakeName(e.Argument)}) bits.Add(\"{e.Argument}\");");
    }

    var definition = $$"""
                       public static string GetEncodedValue({{name}} value)
                       {
                           List<string> bits = new List<string>();
                           {{Statement.Indent(string.Join("\n", cases))}}
                           return string.Join(" ", bits);
                       }
                       public static string GetEncodedValue({{name}}? value) => GetEncodedValue(value!.Value!);
                       {{statement.DescriptionString}}{{statement.AttributeString}}
                       public enum {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", strings))}}
                       }
                       """;
    return (name, definition);
});