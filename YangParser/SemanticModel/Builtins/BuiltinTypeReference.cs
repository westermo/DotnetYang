using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Generator;

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
        Type type = statement is Type type1 ? type1 : statement.GetChild<Type>();
        var baseType = type.GetBaseType(out var prefix, out var chosen);
        string? parseFunction;
        string toString = "WrittenValue.ToString()!";
        switch (baseType)
        {
            case "union":
                parseFunction = $$"""
                                  public static {{typeName}} Parse(string value)
                                  {
                                     return new {{typeName}}({{baseTypeName}}.Parse(value));
                                  }
                                  """;
                break;
            case "leafref":
                parseFunction = $$"""
                                  public static {{typeName}} Parse(string value)
                                  {
                                     return new {{typeName}}(new {{baseTypeName}}());
                                  }
                                  """;
                break;
            case "bits":
            case "enumeration":
            case "identityref":
                _ = baseTypeName.Prefix(out var local);
                var call = $"Get{local}Value";
                if (chosen.Name == null || !chosen.Name.EndsWith(local))
                {
                    toString = "GetEncodedValue(WrittenValue)";
                }

                if (string.IsNullOrEmpty(prefix))
                {
                    //Is local reference.
                    if (!IsBuiltinKeyword(type.Argument))
                    {
                        call = "YangNode." + call;
                    }
                }
                else
                {
                    //Is imported reference
                    var p = prefix.Contains('.') ? prefix : prefix + ":";
                    call = p + call;
                }

                parseFunction = $$"""
                                  public static {{typeName}} Parse(string value)
                                  {
                                     return new {{typeName}}({{call}}(value));
                                  }
                                  """;
                break;
            default:
                parseFunction = $$"""
                                  public static {{typeName}} Parse(string value)
                                  {
                                     return new {{typeName}}({{baseTypeName}}.Parse(value)); //{{baseType}} parsing;
                                  }
                                  """;
                break;
        }

        if (baseTypeName is "string")
        {
            parseFunction = $$"""
                              public static {{typeName}} Parse(string value)
                              {
                                 return new {{typeName}}(value);
                              }
                              """;
        }

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
                        return {{toString}};
                     }
                     {{Statement.Indent(parseFunction)}}
                 }
                 """;
    }

    private static string GetText(string argument) => $$"""
                                                        await reader.ReadAsync();
                                                        while(reader.NodeType == XmlNodeType.Whitespace) await reader.ReadAsync();
                                                        if(reader.NodeType != XmlNodeType.Text)
                                                        {
                                                            throw new Exception($"Expected token in ParseCall for '{{argument}}' to be text, but was '{reader.NodeType}'");
                                                        }
                                                        """;

    private static string EndElement(string argument) => $$"""
                                                           if(!reader.IsEmptyElement)
                                                           {
                                                               
                                                               await reader.ReadAsync();
                                                               while(reader.NodeType == XmlNodeType.Whitespace) await reader.ReadAsync();
                                                               if(reader.NodeType != XmlNodeType.EndElement)
                                                               {
                                                                   throw new Exception($"Expected token in ParseCall for '{{argument}}' to be an element closure, but was '{reader.NodeType}'");
                                                               }
                                                           }
                                                           """;

    public static string ValueTransformation(Type type, string typeName, string target, string argument)
    {
        if (typeName == "string")
        {
            return $"""
                    {GetText(argument)}
                    {target} = await reader.GetValueAsync();
                    {EndElement(argument)}
                    """;
        }

        if (type.Argument == "leafref")
        {
            return $"""
                    {GetText(argument)}
                    {target} = new {typeName}();
                    {EndElement(argument)}
                    """;
        }

        var baseType = type.GetBaseType(out var prefix, out var chosenType);
        switch (baseType)
        {
            case "union":
                return $"""
                        {GetText(argument)}
                        {target} = {typeName}.Parse(await reader.GetValueAsync());
                        {EndElement(argument)}
                        """;
            case "empty":
                return $"""
                        {target} = new object();
                        {EndElement(argument)}
                        """;
            case "bits":
            case "enumeration":
            case "identityref":
                var altPrefix = typeName.Prefix(out var local);
                if (string.IsNullOrWhiteSpace(prefix)) prefix = altPrefix;
                if (chosenType.Name != null && chosenType.Name.EndsWith(local))
                {
                    if (string.IsNullOrEmpty(prefix))
                    {
                        //Is local reference.
                        return IsBuiltin(type, out _, out _)
                            ? //Is direct subtype
                            $"""
                             {GetText(argument)}
                             {target} = Get{local}Value(await reader.GetValueAsync());
                             {EndElement(argument)}
                             """
                            : $"""
                               {GetText(argument)}
                               {target} = YangNode.Get{local}Value(await reader.GetValueAsync());
                               {EndElement(argument)}
                               """;
                    }

                    //Is imported reference
                    //Is a direct enum/bits reference
                    var p = prefix.Contains('.') ? prefix : prefix + ":";
                    return $"""
                            {GetText(argument)}
                            {target} = {p}Get{local}Value(await reader.GetValueAsync());
                            {EndElement(argument)}
                            """;
                }

                //Is a multiple-level abstraction
                return $"""
                        {GetText(argument)}
                        {target} = {typeName}.Parse(await reader.GetValueAsync());
                        {EndElement(argument)}
                        """;
            default:
                return $"""
                        {GetText(argument)}
                        {target} = {typeName}.Parse(await reader.GetValueAsync());
                        {EndElement(argument)}
                        """;
        }
    }

    public static string ValueParsing(Type type, string typeName)
    {
        if (typeName == "string")
        {
            return $"return value;";
        }

        var baseType = type.GetBaseType(out var prefix, out var chosen);
        switch (baseType)
        {
            case "union":
                return $"return {typeName}.Parse(value);";
            case "empty":
                return "if(string.IsNullOrWhiteSpace(value)) return new object();";
            case "leafref":
                return $"if(string.IsNullOrWhiteSpace(value)) return new {typeName}();";
            case "bits":
            case "enumeration":
            case "identityref":
                _ = typeName.Prefix(out var local);
                if (chosen.Name!.EndsWith(local))
                {
                    if (string.IsNullOrEmpty(prefix))
                    {
                        //Is local reference.
                        return IsBuiltin(type, out _, out _)
                            ? //Is direct subtype
                            $"return Get{local}Value(value);"
                            : $"return YangNode.Get{local}Value(value);";
                    }

                    //Is imported reference
                    var p = prefix.Contains('.') ? prefix : prefix + ":";
                    return $"return {p}Get{local}Value(value);";
                }
                return $"return {typeName}.Parse(value);";
            default:
                return $"return {typeName}.Parse(value);";
        }
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