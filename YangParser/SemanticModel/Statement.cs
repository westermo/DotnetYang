using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    private static string Capitalize(string section)
    {
        var first = section[0];
        var rest = section.Substring(1, section.Length - 1);
        return char.ToUpperInvariant(first) + rest;
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

    protected static string MakeName(string argument)
    {
        var output = new StringBuilder(argument.Length);
        var prefixing = argument.Split(':');
        argument = prefixing.Last();
        foreach (var section in argument.Split('-'))
        {
            output.Append(Capitalize(section));
        }

        prefixing[prefixing.Length - 1] = output.ToString();
        var result = string.Join(":", prefixing);
        return result;
    }

    protected string Print(YangStatement statement, int indent = 0)
    {
        StringBuilder output = new StringBuilder();
        var terminator = statement.Children.Count == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.AppendLine($"{tabs}{statement.Prefix}{statement.Keyword} {statement.Argument}{terminator}");
        if (statement.Children.Count <= 0) return output.ToString();
        output.AppendLine($"{tabs}{{");
        foreach (var sub in statement.Children) output.AppendLine(Print(sub, indent + 1));
        output.AppendLine($"{tabs}}}");
        return output.ToString();
    }

    protected string Print(IStatement statement, int indent = 0)
    {
        StringBuilder output = new StringBuilder();
        var terminator = statement.Children.Length == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.AppendLine($"{tabs}{statement.GetType().Name} {statement.Argument}{terminator}");
        if (statement.Children.Length <= 0) return output.ToString();
        output.AppendLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
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

    protected static string Indent(string source)
    {
        return source.Replace("\n", "\n\t");
    }

    protected static string TypeName(Type type)
    {
        if (!BuiltinTypeReference.IsBuiltin(type, out var corresponding)) return MakeName(type.Argument);
        return corresponding ?? MakeName(type.Argument);
    }

    protected string KeywordString => " " + string.Join(" ", Keywords) + (Keywords.Count > 0 ? " " : "");

    protected string AttributeString => string.Join("\n", Attributes.Select(attr => $"[{attr}]"));

    protected string DescriptionString => $"""
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

    public string Argument { get; protected set; } = string.Empty;
    public Dictionary<string, IStatement> GroupingDictionary { get; } = new();
    public virtual ChildRule[] PermittedChildren { get; } = [];
    public List<string> Attributes { get; } = [];
    public List<string> Keywords { get; } = [];

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
        return $"#error ToCode() call on non-overriden type {GetType()}";
    }
}