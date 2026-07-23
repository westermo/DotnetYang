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
        var stateSourceNames = new HashSet<string>(
            Children.OfType<IXMLSource>()
                .Where(t => t.Attributes.Contains("NotConfigurationData"))
                .Select(t => t.TargetName!));
        var stateElements = new HashSet<IXMLWriteValue>(
            Children.OfType<IXMLWriteValue>()
                .Where(t => t.Attributes.Contains("NotConfigurationData")));

        var guardedWriteCalls = Children.OfType<IXMLSource>()
            .Select(t => stateSourceNames.Contains(t.TargetName!)
                ? $"if(!configOnly) {{ if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer, configOnly); }}"
                : $"if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer, configOnly);");
        var guardedElementCalls = Children.OfType<IXMLWriteValue>()
            .Select(t => stateElements.Contains(t)
                ? $"if(!configOnly) {{\n{t.WriteCall}\n}}"
                : t.WriteCall);

        return $$"""
                 public async Task WriteXMLAsync(XmlWriter writer, bool configOnly = false)
                 {
                     await writer.WriteStartElementAsync({{xmlPrefix}},"{{Argument}}",{{xmlNs}});
                     {{Indent(string.Join("\n", guardedElementCalls))}}
                     {{Indent(string.Join("\n", guardedWriteCalls))}}
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
                                     case "rpc-error": throw await RpcException.ParseAsync(reader);
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

    public bool Obsolete()
    {
        if (this.TryGetChild<Status>(out var status))
        {
            return status!.Active;
        }

        return false;

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
            var isMandatory = (child is Leaf leaf && leaf.IsRequired)
                              || (xmlValue.TryGetChild<Mandatory>(out var mandatory) && mandatory!.Value);
            var nullability = isMandatory ? string.Empty : "?";
            if (child is List listChild)
            {
                declarations.Add($"{listChild.CollectionTypeString}{nullability} _{xmlValue.TargetName} = default!;");
            }
            else
            {
                declarations.Add($"{xmlValue.ClassName}{nullability} _{xmlValue.TargetName} = default!;");
            }

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
            var isMandatory = xml.TryGetChild<Mandatory>(out var mandatory) && mandatory!.Value;
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
        var writeCalls = Children.OfType<IXMLSource>().ToArray();
        var elementCalls = Children.OfType<IXMLWriteValue>().ToArray();

        if (elementCalls.Length == 0 && writeCalls.Length == 0)
        {
            return """
                   public async Task WriteXMLAsync(XmlWriter writer, bool configOnly = false)
                   {
                       await writer.FlushAsync();
                   }
                   """;
        }

        var stateSourceNames = new HashSet<string>(
            writeCalls
                .Where(t => t.Attributes.Contains("NotConfigurationData"))
                .Select(t => t.TargetName!));
        var stateElements = new HashSet<IXMLWriteValue>(
            elementCalls
                .Where(t => t.Attributes.Contains("NotConfigurationData")));

        var guardedWriteLines = writeCalls
            .Select(t => stateSourceNames.Contains(t.TargetName!)
                ? $"if(!configOnly) {{ if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer, configOnly); }}"
                : $"if({t.TargetName} is not null) await {t.TargetName}.WriteXMLAsync(writer, configOnly);").ToArray();
        var guardedElementLines = elementCalls
            .Select(t => stateElements.Contains(t)
                ? $"if(!configOnly) {{\n{t.WriteCall}\n}}"
                : t.WriteCall).ToArray();

        return $$"""
                 public async Task WriteXMLAsync(XmlWriter writer, bool configOnly = false)
                 {
                     {{Indent(string.Join("\n", guardedElementLines))}}
                     {{Indent(string.Join("\n", guardedWriteLines))}}
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

    public static string MakeNamespace(string argument)
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

    /// <summary>
    /// Returns the fully qualified C# type name of the nearest enclosing generated class
    /// (the parent of this statement in the generated code tree). Returns <c>null</c> when
    /// this statement would be at module-namespace top level without a containing instance
    /// class.
    /// </summary>
    public string? ParentClassName => ResolveQualifiedClassName(Parent);

    /// <summary>
    /// Returns the fully qualified generated C# class name for any statement that
    /// emits a class (Container, List entry, Choice, Case, Input, Output,
    /// Notification, ExtensionReference, Module), walking outwards through
    /// nested classes; returns <c>null</c> if the statement does not map to a
    /// generated class.
    /// </summary>
    public static string? ResolveQualifiedClassName(IStatement? statement)
    {
        if (statement is null) return null;
        var chain = new List<string>();
        string? rootNamespace = null;
        var p = statement;
        while (p is not null)
        {
            switch (p)
            {
                case Container c: chain.Add(c.ClassName); break;
                case List l: chain.Add(l.ClassName); break;
                case Choice ch: chain.Add(ch.ClassName); break;
                case Case cs: chain.Add(cs.ClassName); break;
                case Input i: chain.Add(i.ClassName); break;
                case Output o: chain.Add(o.ClassName); break;
                // Action: its Input/Output are siblings, not nested in its class.
                case Action: break;
                case Notification n: chain.Add(n.ClassName); break;
                case ExtensionReference er: chain.Add(er.ClassName); break;
                case Module m:
                    chain.Add("YangNode");
                    rootNamespace = MakeNamespace(m.Argument);
                    break;
            }
            if (rootNamespace != null) break;
            p = p.Parent;
        }
        if (chain.Count == 0) return null;
        if (rootNamespace == null) return chain[0];
        chain.Reverse();
        return "global::" + rootNamespace + "." + string.Join(".", chain);
    }

    /// <summary>
    /// Emits a strongly-typed tree-parent property for a generated class, or an empty
    /// string if no parent class is resolvable. The property is named <c>YangParent</c>
    /// (not <c>Parent</c>) to avoid colliding with any YANG identifier named "parent".
    /// </summary>
    protected string ParentPropertyDeclaration()
    {
        var parentName = ParentClassName;
        if (parentName is null)
        {
            return "YangSupport.IYangNode? YangSupport.IYangNode.YangParent => null;";
        }
        return $$"""
                 public {{parentName}}? YangParent { get; internal set; }
                 YangSupport.IYangNode? YangSupport.IYangNode.YangParent => YangParent;
                 """;
    }

    /// <summary>
    /// Generates a GetChild(string yangName) method that maps YANG element names
    /// to C# property values. Used for instance-identifier resolution at runtime.
    /// </summary>
    protected string GetChildMethod()
    {
        var cases = new List<string>();
        var seenNames = new HashSet<string>();
        foreach (var child in Children)
        {
            string? targetName = null;
            switch (child)
            {
                case Container c: targetName = c.TargetName; break;
                case List l: targetName = l.TargetName; break;
                case Leaf lf: targetName = lf.TargetName; break;
                case LeafList ll when !string.IsNullOrEmpty(ll.TargetName): targetName = ll.TargetName; break;
                case Choice ch: targetName = MakeName(ch.Argument); break;
                case AnyXml ax: targetName = MakeName(ax.Argument); break;
                case AnyData ad: targetName = MakeName(ad.Argument); break;
            }
            if (string.IsNullOrEmpty(targetName)) continue;
            var yangName = child.Argument;
            if (!seenNames.Add(yangName)) continue; // skip duplicates
            cases.Add($"\"{yangName}\" => {targetName},");
        }

        if (cases.Count == 0)
        {
            return """
                   public object? GetChild(string yangName) => null;
                   """;
        }

        return $$"""
                 public object? GetChild(string yangName) => yangName switch
                 {
                     {{Indent(string.Join("\n", cases))}}
                     _ => null
                 };
                 """;
    }

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
                case Cardinality.OneOrMore when occurrences.TryGetValue(allowed.Keyword, out var count):
                {
                    if (count >= 1) break;
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} must exist at least once in {GetType()}", statement);
                }
                case Cardinality.OneOrMore:
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} must exist at least once in {GetType()}", statement);
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