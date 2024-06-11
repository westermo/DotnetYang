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

    var writeCases = new List<string>();
    var readCases = new List<string>();
    foreach (var e in bits)
    {
        writeCases.Add(
            $"if ((value & {name}.{Statement.MakeName(e.Argument)}) == {name}.{Statement.MakeName(e.Argument)}) bits.Add(\"{e.Argument}\");");
        readCases.Add(
            $"""
             case "{e.Argument}":
                 output ??= {name}.{Statement.MakeName(e.Argument)};
                 output |= {name}.{Statement.MakeName(e.Argument)};
                 break;
             """);
    }

    var definition = $$"""
                       public static string GetEncodedValue({{name}} value)
                       {
                           List<string> bits = new List<string>();
                           {{Statement.Indent(string.Join("\n", writeCases))}}
                           return string.Join(" ", bits);
                       }
                       public static {{name}} Get{{name}}Value(string value)
                       {
                           {{name}}? output = null;
                           foreach(var component in value.Split(' '))
                           {
                               switch(component)
                               {
                                   {{Statement.Indent(Statement.Indent(Statement.Indent(string.Join("\n", readCases))))}}
                                   default: throw new Exception($"{component} is not a valid value for {{name}}");
                               }
                           }
                           if(output is null)
                           {
                               throw new Exception($"No value was assigned on decoding of {{name}} from {value}");
                           }
                           return output.Value!;
                           
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