using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public static class BuiltinTypeReference
{
    private static readonly List<BuiltinType> m_builtIns =
    [
        new Binary(),
        new Bits(),
        new Boolean(),
        new Decimal64(),
        new Empty(),
        new Enumeration(),
        new IdentityReference(),
        new InstanceIdentifier(),
        new Int8(),
        new Int16(),
        new Int32(),
        new Int64(),
        new LeafReference(),
        new String(),
        new Uint8(),
        new Uint16(),
        new Uint32(),
        new Uint64(),
        new Union()
    ];

    public static bool IsBuiltin(Type type, out string? cSharpType, out string? definition)
    {
        cSharpType = null;
        definition = null;
        var comparison = type.Argument.Split(':', '.').Last();
        foreach (var builtin in m_builtIns)
        {
            if (builtin.Name != comparison) continue;
            (cSharpType, definition) = builtin.CorrespondingCSharpType(type);
            return true;
        }

        return false;
    }

    public static bool IsBuiltinKeyword(string keyword)
    {
        return m_builtIns.Any(b => b.Name == keyword);
    }

    public static string DefaultPattern(IStatement statement, IEnumerable<string> staticFields,
        IEnumerable<string> constructorStatements,
        string baseTypeName, string typeName)
    {
        return $$"""
                 {{statement.DescriptionString}}{{statement.AttributeString}}
                 public class {{typeName}}
                 {
                     {{Statement.Indent(string.Join("\n", staticFields))}}
                     public {{baseTypeName}} WrittenValue { get; }
                     public static implicit operator {{baseTypeName}}?({{typeName}}? input) => input?.WrittenValue;
                     public static implicit operator {{typeName}}({{baseTypeName}} input) => new {{typeName}}(input);
                     public {{typeName}}({{baseTypeName}} input)
                     {
                         {{Statement.Indent(Statement.Indent(string.Join("\n", constructorStatements)))}}
                         WrittenValue = input;
                     }
                     public override string ToString()
                     {
                        return WrittenValue.ToString()!;
                     }
                 }
                 """;
    }

    public static string TypeName(IStatement type)
    {
        string Postfix = string.Empty;
        var parent = type.Parent!;
        while (IsBuiltinKeyword(parent.Argument))
        {
            Postfix += Array.IndexOf(parent.Children, type);
            parent = parent.Parent!;
        }

        return Statement.MakeName(parent.Argument) + Postfix;
    }
}