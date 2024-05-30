using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Union() : BuiltinType("union", s =>
{
    var options = s.Children.OfType<Type>().ToArray();
    var sourceName = BuiltinTypeReference.TypeName(s);
    List<string> types = [];
    List<string> declarations = [];
    foreach (var option in options)
    {
        var typeName = option.Name!;
        types.Add(typeName);
        if (option.Definition != null)
        {
            var declaration = option.Definition;
            declarations.Add(declaration);
        }
    }

    var varName = (string t) => Statement.Capitalize(t).Split(':', '.').Last() + "Value";
    var name = sourceName;
    var definition = $$"""
                       {{s.DescriptionString}}{{s.AttributeString}}
                       public class {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public {name}({typeName} input){{ {varName(typeName)} = input; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public {typeName}? {varName(typeName)} {{ get; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {typeName}?({name}? input) => input is null ? null : input.{varName(typeName)} ?? throw new InvalidOperationException(\"Union was not of effective type '{typeName}'\");")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {name}({typeName} input) => new {name}(input);")))}}
                           {{Statement.Indent(string.Join("\n", declarations))}}
                       }
                       """;
    return (name, definition);
});