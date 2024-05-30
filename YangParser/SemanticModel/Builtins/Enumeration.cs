using System.Collections.Generic;
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

    var cases = new List<string>();
    foreach (var e in enums)
    {
        cases.Add($"case {name}.{Statement.MakeName(e.Argument)}: return \"{e.Argument}\";");
    }

    var definition = $$"""
                       public static string GetEncodedValue({{name}} value)
                       {
                           switch(value)
                           {
                               {{Statement.Indent(Statement.Indent(string.Join("\n", cases)))}}
                               default: return value.ToString();
                           }
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