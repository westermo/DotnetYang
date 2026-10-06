using System.Collections.Generic;

namespace YangParser.SemanticModel;

/// <summary>
/// Emits the C# enum and conversion helpers (GetEncodedValue / GetIdentityNamespace /
/// Get{Enum}Value) for a set of YANG identities.
/// </summary>
internal static class IdentityEnumEmitter
{
    /// <param name="className">Name of the emitted enum.</param>
    /// <param name="identities">Valid identity values, in declaration order.</param>
    /// <param name="descriptionAndAttributes">Doc-comment and attribute block emitted in front of the enum.</param>
    /// <param name="includeNullableOverloads">Also emit overloads accepting a nullable enum value.</param>
    public static string Emit(string className, IEnumerable<Identity> identities, string descriptionAndAttributes,
        bool includeNullableOverloads)
    {
        HashSet<string> conversionSet = [];
        HashSet<string> backConversionSet = [];
        Dictionary<string, string> namespaceMap = [];
        HashSet<string> declarationSet = [];
        foreach (var validValue in identities)
        {
            var argName = Statement.MakeName(validValue.Argument);
            conversionSet.Add($"case {className}.{argName}: return \"{validValue.Argument}\";");
            backConversionSet.Add($"case \"{validValue.Argument}\": return {className}.{argName};");
            // Diamond inheritance can yield the same identity twice; keep the first namespace mapping.
            if (!namespaceMap.ContainsKey(argName))
            {
                var identityModule = validValue.GetModule() as Module;
                var identityNs = identityModule?.XmlNamespace?.Namespace ?? string.Empty;
                namespaceMap[argName] = $"case {className}.{argName}: return \"{identityNs}\";";
            }

            declarationSet.Add(argName);
        }

        var nullableEncoded = includeNullableOverloads
            ? $"\npublic static string GetEncodedValue({className}? value) => GetEncodedValue(value!.Value!);"
            : string.Empty;
        var nullableNamespace = includeNullableOverloads
            ? $"\npublic static string GetIdentityNamespace({className}? value) => GetIdentityNamespace(value!.Value!);"
            : string.Empty;

        return $$"""
                 public static string GetEncodedValue({{className}} value)
                 {
                     switch(value)
                     {
                         {{Statement.Indent(Statement.Indent(string.Join("\n", conversionSet)))}}
                         default: return value.ToString();
                     }
                 }{{nullableEncoded}}
                 public static string GetIdentityNamespace({{className}} value)
                 {
                     switch(value)
                     {
                         {{Statement.Indent(Statement.Indent(string.Join("\n", namespaceMap.Values)))}}
                         default: return string.Empty;
                     }
                 }{{nullableNamespace}}
                 public static {{className}} Get{{className}}Value(string value)
                 {
                     // Strip any namespace prefix (e.g., "ianaift:ethernetCsmacd" -> "ethernetCsmacd")
                     var colonIndex = value.IndexOf(':');
                     if(colonIndex >= 0) value = value.Substring(colonIndex + 1);
                     switch(value)
                     {
                         {{Statement.Indent(Statement.Indent(string.Join("\n", backConversionSet)))}}
                         default: throw new Exception($"{value} is not a valid value for {{className}}");
                     }
                 }
                 {{descriptionAndAttributes}}
                 public enum {{className}}
                 {
                     {{Statement.Indent(string.Join(",\n", declarationSet))}}
                 }
                 """;
    }
}
