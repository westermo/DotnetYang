using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Union() : BuiltinType("union", s =>
{
    var options = s.Children.OfType<Type>().ToArray();
    var sourceName = BuiltinTypeReference.TypeName(s);
    List<string> types = [];
    List<string> declarations = [];
    List<string> switches = [];
    List<string> stringifications = [];
    foreach (var option in options)
    {
        var typeName = option.Name!;
        types.Add(typeName);

        if (option.Definition != null)
        {
            var declaration = option.Definition;
            declarations.Add(declaration);
        }

        switches.Add($$"""
                       try { {{BuiltinTypeReference.ValueParsing(option, typeName)}} } catch(Exception ex) {  errors += " " + ex.Message; }
                       """);
        stringifications.Add(
            $"if({VariableName(typeName)} is not null) return {BuiltinTypeReference.Stringification(option, VariableName(typeName))};");
    }

    var name = sourceName;
    var definition = $$"""
                       {{s.DescriptionString}}{{s.AttributeString}}
                       public class {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public {name}({typeName} input){{ {VariableName(typeName)} = input; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public {typeName}? {VariableName(typeName)} {{ get; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {typeName}?({name}? input) => input is null ? null : input.{VariableName(typeName)} ?? throw new InvalidOperationException(\"Union was not of effective type '{typeName}'\");")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {name}({typeName} input) => new {name}(input);")))}}
                           {{Statement.Indent(string.Join("\n", declarations))}}
                           public override string? ToString()
                           {
                               {{Statement.Indent(Statement.Indent(string.Join("\n", stringifications)))}}
                               return string.Empty;
                           }
                           public static {{name}} Parse(string value)
                           {
                               string errors = string.Empty;
                               {{Statement.Indent(Statement.Indent(string.Join("\n", switches)))}}
                               throw new Exception("Failed to parse {{name}}, no match for any subtype, the following errors occured:" + errors);
                           }
                       }
                       """;
    return (name, definition);

    string VariableName(string t) => Statement.Capitalize(t).Split(':', '.').Last() + "Value";
});