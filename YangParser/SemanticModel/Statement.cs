using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public abstract class Statement : IStatement
{
    protected string xmlPrefix => string.IsNullOrWhiteSpace(Prefix) || string.IsNullOrWhiteSpace(Namespace)
        ? "null"
        : $"\"{Prefix}\"";

    protected string xmlNs => string.IsNullOrWhiteSpace(Namespace) ? "null" : $"\"{Namespace}\"";

    protected string WriteFunction()
    {
        var writeCalls = Children.OfType<IXMLSource>()
            .Select(t => $"if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer);");
        var elementCalls = Children.OfType<IXMLWriteValue>()
            .Select(t => t.WriteCall);
        return $$"""
                 public async Task WriteXMLAsync(XmlWriter writer)
                 {
                     await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                     {{Indent(string.Join("\n", elementCalls))}}
                     {{Indent(string.Join("\n", writeCalls))}}
                     await writer.WriteEndElementAsync();
                 }
                 """;
    }

    protected string ReadFunction()
    {
        var type = string.Empty;
        switch (this)
        {
            case IXMLReadValue xmlReadValue:
                type = xmlReadValue.ClassName;
                break;
            case IXMLParseable xmlReadValue:
                type = xmlReadValue.ClassName;
                break;
        }

        if (type == string.Empty)
        {
            throw new InvalidOperationException($"ReadFunction called from invalid provider {GetType()}");
        }

        return ReadFunction(type);
    }

    protected string ReadFunction(string type)
    {
        var declarations = new List<string>();
        var assignments = new List<string>();
        var cases = new List<string>();
        HashSet<string> caseKeywords = [];
        CollectParsingChildren(declarations, assignments, cases, caseKeywords, "continue");

        return $$"""
                 public static async Task<{{type}}> ParseAsync(XmlReader reader)
                 {
                     {{Indent(string.Join("\n", declarations))}}
                     while(await reader.ReadAsync())
                     {
                        switch(reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch(reader.Name)
                                {
                                     {{Indent(Indent(Indent(Indent(Indent(string.Join("\n", cases))))))}}
                                    default: throw new Exception($"Unexpected element '{reader.Name}' under '{{XmlObjectName}}'");
                                }
                            case XmlNodeType.EndElement when reader.Name == "{{XmlObjectName}}":
                                return new {{type}}{
                                    {{Indent(Indent(Indent(Indent(Indent(string.Join("\n", assignments))))))}}
                                };
                            case XmlNodeType.Whitespace: break;
                            default: throw new Exception($"Unexpected node type '{reader.NodeType}' : '{reader.Name}' under '{{XmlObjectName}}'");
                        }
                     }
                     throw new Exception("Reached end-of-readability without ever returning from {{type}}.ParseAsync");
                 }
                 """;
    }

    protected string ReadFunctionWithInvisibleSelf()
    {
        var type = string.Empty;
        switch (this)
        {
            case IXMLReadValue xmlReadValue:
                type = xmlReadValue.ClassName;
                break;
            case IXMLParseable xmlReadValue:
                type = xmlReadValue.ClassName;
                break;
        }

        if (type == string.Empty)
        {
            throw new InvalidOperationException($"ReadFunction called from invalid provider {GetType()}");
        }

        return ReadFunctionWithInvisibleSelf(type);
    }

    private string ReadFunctionWithInvisibleSelf(string type)
    {
        var declarations = new List<string>();
        var assignments = new List<string>();
        var cases = new List<string>();
        HashSet<string> caseKeywords = [];
        CollectParsingChildren(declarations, assignments, cases, caseKeywords, "break");

        if (cases.Count > 0)
        {
            return $$"""
                     public static async global::System.Threading.Tasks.Task<{{type}}> ParseAsync(XmlReader reader)
                     {
                         {{Indent(string.Join("\n", declarations))}}
                         switch(reader.Name)
                         {
                             {{Indent(Indent(string.Join("\n", cases)))}}
                             default: throw new Exception($"Unexpected element '{reader.Name}' under '{{Argument}}'");
                         }
                         return new {{type}}{
                             {{Indent(Indent(string.Join("\n", assignments)))}}
                         };
                     }
                     """;
        }

        return $$"""
                 public static global::System.Threading.Tasks.Task<{{type}}> ParseAsync(XmlReader reader)
                 {
                     return global::System.Threading.Tasks.Task.FromResult(new {{type}}());
                 }
                 """;
    }

    private void CollectParsingChildren(List<string> declarations, List<string> assignments, List<string> cases,
        HashSet<string> caseKeywords,
        string escapeKeyword)
    {
        foreach (var child in Children)
        {
            var hasCondition = child.TryGetChild<When>(out var when);
            var booleanStatement = string.Empty;
            /*hasCondition
                ? $" when string.IsNullOrEmpty(\"{SingleLine(when!.Argument.Replace("\"", "\\\""))}\")
                : string.Empty;
            */
            switch (child)
            {
                case IXMLParseable xml:
                    HandleParseables(declarations, assignments, cases, xml, booleanStatement, escapeKeyword,
                        caseKeywords);

                    break;
                case IXMLReadValue xmlValue:
                    HandleReadValues(declarations, assignments, cases, xmlValue, child, booleanStatement,
                        escapeKeyword, caseKeywords);
                    break;
                case IXMLAction action:
                    if (caseKeywords.Contains(child.Argument)) continue;

                    cases.Add($"""
                               case "{action.XmlObjectName}"{booleanStatement}:
                                   {Indent(action.ParseCall)}
                                   {escapeKeyword};
                               """);
                    caseKeywords.Add(child.XmlObjectName);
                    break;
            }
        }
    }

    private static void HandleReadValues(List<string> declarations, List<string> assignments, List<string> cases,
        IXMLReadValue xmlValue,
        IStatement child, string booleanStatement, string escapeKeyword, HashSet<string> caseKeywords)
    {
        if (xmlValue.TargetName != null)
        {
            var isMandatory = xmlValue.TryGetChild<Mandatory>(out _);
            var nullability = isMandatory ? string.Empty : "?";
            declarations.Add(child is List
                ? $"List<{xmlValue.ClassName}>{nullability} _{xmlValue.TargetName} = default!;"
                : $"{xmlValue.ClassName}{nullability} _{xmlValue.TargetName} = default!;");

            assignments.Add($"{xmlValue.TargetName} = _{xmlValue.TargetName},");
        }

        if (caseKeywords.Contains(xmlValue.XmlObjectName)) return;

        cases.Add($"""
                   case "{xmlValue.XmlObjectName}"{booleanStatement}:
                       {Indent(xmlValue.ParseCall)}
                       {escapeKeyword};
                   """);
        caseKeywords.Add(child.XmlObjectName);
    }

    private static void HandleParseables(ICollection<string> declarations, ICollection<string> assignments,
        ICollection<string> cases,
        IXMLParseable xml,
        string booleanStatement, string escapeKeyword, HashSet<string> caseKeywords)
    {
        if (xml.TargetName != null)
        {
            var isMandatory = xml.TryGetChild<Mandatory>(out _);
            var nullability = isMandatory ? string.Empty : "?";
            declarations.Add($"{xml.ClassName}{nullability} _{xml.TargetName} = default!;");
            assignments.Add($"{xml.TargetName} = _{xml.TargetName},");
        }

        switch (xml)
        {
            case Choice choice:
            {
                HandleChoice(cases, booleanStatement, choice, escapeKeyword, caseKeywords);
                break;
            }
            case Case ChoiceCase:
            {
                HandleCase(cases, booleanStatement, ChoiceCase, escapeKeyword, caseKeywords);
                break;
            }
            default:
                if (caseKeywords.Contains(xml.XmlObjectName)) return;

                cases.Add(
                    $"""
                     case "{xml.XmlObjectName}"{booleanStatement}:
                         _{xml.TargetName} = await {xml.ClassName}.ParseAsync(reader);
                         {escapeKeyword};
                     """);
                caseKeywords.Add(xml.XmlObjectName);
                break;
        }
    }

    private static void HandleCase(ICollection<string> cases, string booleanStatement, Case @case,
        string escapeKeyword, ISet<string> caseKeywords)
    {
        StringBuilder builder = new();
        bool added = false;
        foreach (var c in @case.SubTargets)
        {
            if (caseKeywords.Contains(c)) continue;
            added = true;
            builder.AppendLine($"case \"{c}\"{booleanStatement}:");
            caseKeywords.Add(c);
        }

        if (!added)
        {
            if (caseKeywords.Contains(@case.XmlObjectName)) return;
            builder.AppendLine($"case \"{@case.XmlObjectName}\"{booleanStatement}:");
            caseKeywords.Add(@case.XmlObjectName);
        }

        builder.AppendLine($"""
                                _{@case.TargetName} = await {@case.ClassName}.ParseAsync(reader);
                                {escapeKeyword};
                            """);
        cases.Add(builder.ToString());
    }

    private static void HandleChoice(ICollection<string> cases, string booleanStatement, Choice choice,
        string escapeKeyword, ISet<string> caseKeywords)
    {
        StringBuilder builder = new();
        bool added = false;
        foreach (var c in choice.SubTargets)
        {
            if (caseKeywords.Contains(c)) continue;
            added = true;
            builder.AppendLine($"case \"{c}\"{booleanStatement}:");
            caseKeywords.Add(c);
        }

        if (!added)
        {
            if (caseKeywords.Contains(choice.XmlObjectName)) return;
            builder.AppendLine($"case \"{choice.XmlObjectName}\"{booleanStatement}:");
            caseKeywords.Add(choice.XmlObjectName);
        }

        builder.AppendLine($"""
                                _{choice.TargetName} = await {choice.ClassName}.ParseAsync(reader);
                                {escapeKeyword};
                            """);
        cases.Add(builder.ToString());
    }

    protected string WriteFunctionInvisibleSelf()
    {
        var writeCalls = Children.OfType<IXMLSource>()
            .Select(t => $"if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer);").ToArray();
        var elementCalls = Children.OfType<IXMLWriteValue>()
            .Select(t => t.WriteCall).ToArray();
        if (elementCalls.Length == 0 && writeCalls.Length == 0)
        {
            return """
                   public async Task WriteXMLAsync(XmlWriter writer)
                   {
                       await writer.FlushAsync();
                   }
                   """;
        }

        return $$"""
                 public async Task WriteXMLAsync(XmlWriter writer)
                 {
                     {{Indent(string.Join("\n", elementCalls))}}
                     {{Indent(string.Join("\n", writeCalls))}}
                 }
                 """;
    }

    protected Statement(YangStatement statement, bool validate = true)
    {
        Argument = statement.Argument?.ToString() ?? string.Empty;
        Metadata = statement.Metadata;
        Source = statement;
        if (validate)
        {
            ValidateChildren(statement);
        }

        Children = statement.Children.Select(StatementFactory.Create).ToArray();
    }

    public (string Namespace, string Prefix)? XmlNamespace { get; set; }
    public string Prefix => XmlNamespace?.Prefix ?? Parent?.Prefix ?? string.Empty;
    public string Namespace => XmlNamespace?.Namespace ?? Parent?.Namespace ?? string.Empty;
    public string XmlObjectName => (string.IsNullOrEmpty(Prefix) ? string.Empty : Prefix + ":") + Argument;

    public string XPath => ((Parent?.XPath ?? string.Empty) + "/").Replace("//", "/") +
                           XmlObjectName;

    public YangStatement Source { get; set; }

    private IStatement[] _children = [];
    private IStatement? _parent;

    public static string Capitalize(string section)
    {
        if (section.Length == 0) return section;
        if (section.Length == 1)
        {
            if (section[0] is >= '0' and <= '9')
            {
                return '_' + section;
            }

            return section.ToUpperInvariant();
        }

        var first = section[0];
        var rest = section.Substring(1, section.Length - 1);
        if (first is >= '0' and <= '9')
        {
            return '_' + section;
        }

        return (char.ToUpperInvariant(first) + rest);
    }

    protected static string MakeNamespace(string argument)
    {
        var output = new StringBuilder(argument.Length);
        foreach (var section in argument.Split('-'))
        {
            output.Append(Capitalize(section));
            if (output.Length < argument.Length) output.Append('.');
        }

        return output.ToString();
    }

    public static string MakeName(string argument)
    {
        var output = new StringBuilder(argument.Length);
        var versioning = VersionIndicator.Match(argument);
        if (versioning.Success)
        {
            var component = versioning.Groups["target"].Value;
            var replacement = component.Replace(".", "dot");
            argument = argument.Replace(component, replacement);
        }

        var prefix = argument.Prefix(out var value);
        var addColon = !prefix.Contains('.') && !string.IsNullOrWhiteSpace(prefix);
        foreach (var section in value.Split('-', ' ', '/', '.', '^'))
        {
            output.Append(Capitalize(section));
        }

        var result = (prefix + (addColon ? ":" : "") + output).Replace("*", "Any");
        return result;
    }

    public override string ToString()
    {
        StringBuilder output = new StringBuilder();
        var terminator = Children.Length == 0 ? ";" : "";
        var tabs = new StringBuilder();
        output.AppendLine($"{tabs}{GetType().Name} {Argument}{terminator}");
        if (Children.Length <= 0) return output.ToString();
        output.AppendLine($"{tabs}{{");
        foreach (var sub in Children) output.AppendLine("\t" + Indent(sub.ToString()));
        output.AppendLine($"{tabs}}}");
        return output.ToString();
    }

    public static string IndentLevel(string source, int level)
    {
        for (int i = 0; i < level; i++)
        {
            source = Indent(source);
        }

        return source;
    }

    public static string Indent(string source)
    {
        return source.Replace("\n", "\n\t");
    }

    public static readonly Regex VersionIndicator = new(@"(?<target>[0-9]+(\.[0-9]+)+)");

    protected static string TypeName(Type type)
    {
        if (!BuiltinTypeReference.IsBuiltin(type, out var corresponding, out _))
            return MakeName(type.Argument);
        return corresponding ?? MakeName(type.Argument);
    }

    protected string KeywordString => " " + string.Join(" ", Keywords) + (Keywords.Count > 0 ? " " : "");

    public string AttributeString
    {
        get { return "\n" + string.Join("\n", Attributes.OrderBy(x => x.Length).Select(attr => $"[{attr}]")); }
    }

    public string DescriptionString => Children.FirstOrDefault(c => c is Description) is Description description
        ? $"""
           ///<summary>
           ///{description.Argument.Replace("\n", "\n///")}
           ///</summary>
           """
        : string.Empty;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statement"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected void ValidateChildren(YangStatement statement)
    {
        Dictionary<string, int> occurrences = new();
        foreach (var child in statement.Children)
        {
            if (!string.IsNullOrWhiteSpace(child.Prefix)) continue;
            if (PermittedChildren.Any(p => p.Keyword == child.Keyword))
            {
                if (occurrences.ContainsKey(child.Keyword))
                {
                    occurrences[child.Keyword]++;
                }
                else
                {
                    occurrences[child.Keyword] = 1;
                }

                continue;
            }

            throw new SemanticError(
                $"Child of type {child.Keyword} is not permitted inside statement of type {GetType()}", statement);
        }

        foreach (var allowed in PermittedChildren)
        {
            switch (allowed.Cardinality)
            {
                case Cardinality.Required when occurrences.TryGetValue(allowed.Keyword, out var count):
                {
                    if (count == 1) break;
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} can only exist once in {GetType()}", statement);
                }
                case Cardinality.Required:
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} must exist in type {GetType()}", statement);
                case Cardinality.ZeroOrOne when occurrences.TryGetValue(allowed.Keyword, out var count):
                {
                    if (count <= 1) break;
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} can only exist up to once in {GetType()}", statement);
                }
                case Cardinality.ZeroOrOne:
                case Cardinality.ZeroOrMore:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(allowed.Cardinality.ToString());
            }
        }
    }

    public string Argument { get; set; }
    public virtual ChildRule[] PermittedChildren { get; } = [];
    public HashSet<string> Attributes { get; } = [];
    public HashSet<string> Keywords { get; } = [];

    public IStatement[] Children
    {
        get => _children;
        set
        {
            _children = value;
            foreach (var child in _children)
            {
                child.Parent = this;
            }
        }
    }

    public void Replace(IStatement child, IEnumerable<IStatement> replacements)
    {
        var replace = replacements.ToArray();
        var children = Children.ToList();
        children.Remove(child);
        Children = Merge(children, replace);
    }

    public void Insert(IEnumerable<IStatement> augments)
    {
        Children = Merge(Children.ToList(), augments);
    }

    private static IStatement[] Merge(List<IStatement> first, IEnumerable<IStatement> second)
    {
        foreach (var insertion in second)
        {
            var inserted = false;
            foreach (var original in first.ToArray())
            {
                if (original.Argument != insertion.Argument) continue;
                if (original.GetType() == insertion.GetType() || insertion is Container { IsPlaceholder: true })
                {
                    original.Children = Merge(original.Children.ToList(), insertion.Children);
                    inserted = true;
                    break;
                }

                if (original is not Container { IsPlaceholder: true }) continue;
                insertion.Children = Merge(insertion.Children.ToList(), original.Children);
                first.Remove(original);
                break;
            }

            if (!inserted)
            {
                first.Add(insertion);
            }
        }

        return first.ToArray();
    }

    public IStatement? Parent
    {
        get => _parent;
        set
        {
            _parent = value;
            ValidateParent();
        }
    }

    protected virtual void ValidateParent()
    {
    }

    public Metadata Metadata { get; }

    public virtual string ToCode()
    {
        return $"#warning ToCode() call on non-overriden type {GetType()}:\n/*{this}\n*/";
    }

    public static string SingleLine(string multiline, string separator = " ")
    {
        return string.Join(separator, multiline.Split('\n').Select(s => s.Trim()));
    }
}