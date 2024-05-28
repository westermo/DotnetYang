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
        foreach (var section in value.Split('-', ' ', '/', '.'))
        {
            output.Append(Capitalize(section));
        }
        var result = (prefix + (addColon ? ":" : "") + output.ToString()).Replace("*", "Any");
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

    private static Regex VersionIndicator = new(@"(?<target>[0-9]+(\.[0-9]+)+)");

    protected static string TypeName(Type type)
    {
        if (!BuiltinTypeReference.IsBuiltin(type, out var corresponding, out var definition))
            return MakeName(type.Argument);
        return corresponding ?? MakeName(type.Argument);
    }

    protected string KeywordString => " " + string.Join(" ", Keywords) + (Keywords.Count > 0 ? " " : "");

    public string AttributeString => Attributes.Count > 0
        ? "\n" + string.Join("\n", Attributes.OrderBy(x => x.Length).Select(attr => $"[{attr}]"))
        : string.Empty;

    public string DescriptionString => $"""
                                        ///<summary>
                                        ///{Children.FirstOrDefault(child => child is Description)?.Argument.Replace("\n", "\n///")}
                                        ///</summary>
                                        """;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statement"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected void ValidateChildren(YangStatement statement)
    {
        Dictionary<string, int> occurances = new();
        foreach (var child in statement.Children)
        {
            if (!string.IsNullOrWhiteSpace(child.Prefix)) continue;
            if (PermittedChildren.Any(p => p.Keyword == child.Keyword))
            {
                if (occurances.ContainsKey(child.Keyword))
                {
                    occurances[child.Keyword]++;
                }
                else
                {
                    occurances[child.Keyword] = 1;
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
                case Cardinality.Required when occurances.TryGetValue(allowed.Keyword, out var count):
                    {
                        if (count == 1) break;
                        throw new SemanticError(
                            $"Child of type {allowed.Keyword} can only exist once in {GetType()}", statement);
                    }
                case Cardinality.Required:
                    throw new SemanticError(
                        $"Child of type {allowed.Keyword} must exist in type {GetType()}", statement);
                case Cardinality.ZeroOrOne when occurances.TryGetValue(allowed.Keyword, out var count):
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

    public string Argument { get; set; } = string.Empty;
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

    private IStatement[] Merge(List<IStatement> first, IEnumerable<IStatement> second)
    {
        foreach (var insertion in second)
        {
            var inserted = false;
            foreach (var original in first)
            {
                if (original.Argument == insertion.Argument && original.GetType() == insertion.GetType())
                {
                    original.Children = Merge(original.Children.ToList(), insertion.Children);
                    inserted = true;
                    break;
                }
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

    public static string InterfaceName(Base b)
    {
        return InterfaceName(b.Argument);
    }

    public static string InterfaceName(string arg)
    {
        var expanded = !arg.Contains(':');
        var components = MakeName(arg).Split(':', '.');
        components[components.Length - 1] = "I" + components[components.Length - 1];
        return string.Join(expanded ? "." : ":", components);
    }

    public static string SingleLine(string multiline, string separator = " ")
    {
        return string.Join(separator, multiline.Split('\n').Select(s => s.Trim()));
    }
}