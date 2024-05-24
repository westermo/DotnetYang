using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Union() : BuiltinType("union", s =>
{
    var options = s.Children.OfType<Type>().ToArray();
    var sourceName = Statement.MakeName(s.Parent!.Argument);
    List<string> types = [];
    List<string> declarations = [];
    foreach (var option in options)
    {
        types.Add(option.Name!.Replace("Union", sourceName + Array.IndexOf(options, option)));
        if (option.Definition != null)
        {
            declarations.Add(option.Definition.Replace("Union", sourceName + Array.IndexOf(options, option)));
        }
    }

    var varName = (string t) => Statement.Capitalize(t).Split(':').Last() + "Value";
    var name = sourceName;
    var definition = $$"""
                       {{s.DescriptionString}}{{s.AttributeString}}
                       public class {{name}}
                       {
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"private {name}({typeName} input){{ {varName(typeName)} = input; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public {typeName}? {varName(typeName)} {{ get; }}")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {typeName}({name} input) => input.{varName(typeName)} ?? throw new InvalidOperationException(\"Union was not of effective type '{typeName}'\");")))}}
                           {{Statement.Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {name}({typeName} input) => new {name}(input);")))}}
                           {{Statement.Indent(string.Join("\n", declarations))}}
                       }
                       """;
    return (name, definition);
});