using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Enumeration() : BuiltinType("enumeration", statement =>
{
    var name = BuiltinTypeReference.TypeName(statement);
    var enums = statement.Children.OfType<Enum>().ToArray();
    var others = statement.Children.Except(enums);
    var strings = enums.Select(e => e.ToCode());
    foreach (var child in others)
    {
        child.ToCode();
    }

    var writeCases = new List<string>();
    var readCases = new List<string>();
    foreach (var e in enums)
    {
        writeCases.Add($"case {name}.{Statement.MakeName(e.Argument)}: return \"{e.Argument}\";");
        readCases.Add($"case \"{e.Argument}\": return {name}.{Statement.MakeName(e.Argument)};");
    }

    var definition = $$"""
                       public static string GetEncodedValue({{name}} value)
                       {
                           switch(value)
                           {
                               {{Statement.Indent(Statement.Indent(string.Join("\n", writeCases)))}}
                               default: return value.ToString();
                           }
                       }
                       public static {{name}} Get{{name}}Value(string value)
                       {
                           switch(value)
                           {
                               {{Statement.Indent(Statement.Indent(string.Join("\n", readCases)))}}
                               default: throw new Exception($"{value} is not a valid value for {{name}}");
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