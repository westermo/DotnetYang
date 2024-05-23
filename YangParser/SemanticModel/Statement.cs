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

    protected static string Capitalize(string section)
    {
        if (section.Length == 0) return section;
        if (section.Length == 1) return section.ToUpperInvariant();
        var first = section[0];
        var rest = section.Substring(1, section.Length - 1);
        if (first is >= '0' and <= '9')
        {
            return '_' + section.Replace(".", "_");
        }

        return (char.ToUpperInvariant(first) + rest).Replace(".", "_");
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
        foreach (var section in argument.Split('-', ' ', '/'))
        {
            output.Append(Capitalize(section));
        }

        prefixing[prefixing.Length - 1] = output.ToString();
        var result = string.Join(":", prefixing);
        return result;
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

    protected string AttributeString => Attributes.Count > 0
        ? "\n" + string.Join("\n", Attributes.OrderBy(x => x.Length).Select(attr => $"[{attr}]"))
        : string.Empty;

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

    public string Argument { get; set; } = string.Empty;
    public Dictionary<string, IStatement> GroupingDictionary { get; } = new();
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
        return $"#error ToCode() call on non-overriden type {GetType()}";
    }

    protected string HandleType(Type Input, string typename)
    {
        var name = MakeName(typename);
        if (BuiltinTypeReference.IsBuiltin(Input, out var type))
        {
            return HandleSpecialTypes(Input, name, type);
        }

        return $$"""
                 {{DescriptionString}}{{AttributeString}}
                 public class {{name}}
                 {
                    public {{TypeName(Input)}} Value { get; set; }
                    public static implicit operator {{TypeName(Input)}}({{name}} instance) => instance.Value;
                    public static implicit operator {{name}}({{TypeName(Input)}} value) => new {{name}}{ Value = value };
                 }
                 """;
    }

    protected string HandleSpecialTypes(Type baseType, string name, string? type)
    {
        switch (baseType.Argument.Split(':').Last())
        {
            case "identityref":
            {
                var inherits = baseType.Children.OfType<Base>().Select(InterfaceName).ToArray();
                var inheritance = inherits.Length == 0 ? string.Empty : " : " + string.Join(", ", inherits);
                return $"""
                        {DescriptionString}{AttributeString}
                        public class {name}{inheritance};
                        """;
            }
            case "instance-identifier":
                return $$"""
                         {{DescriptionString}}{{AttributeString}}
                         public class {{name}}(string path) : InstanceIdentifier(path);
                         """;
            case "leafref":
                var path = (Path)baseType.Children.First(c => c is Path);

                return $$"""
                         {{DescriptionString}}{{AttributeString}}
                         public class {{name}}() : InstanceIdentifier("{{path.Argument}}");
                         """;
            case "enumeration":
            {
                var enums = baseType.Children.OfType<Enum>();
                var stringified = enums.Select(e => e.ToCode());
                return $$"""
                         {{DescriptionString}}{{AttributeString}}
                         public enum {{name}}
                         {
                             {{Indent(string.Join("\n", stringified))}}
                         }
                         """;
            }
            case "bits":
            {
                var flags = baseType.Children.OfType<Bit>();
                var stringified = flags.Select(e => e.ToCode());
                return $$"""
                         {{DescriptionString}}{{AttributeString}}
                         [Flags]
                         public enum {{name}}
                         {
                             {{Indent(string.Join("\n", stringified))}}
                         }
                         """;
            }
            case "union":
            {
                var options = baseType.Children.OfType<Type>().ToArray();
                StringBuilder builder = new StringBuilder();
                int i = 0;
                var nameSource = (int j) => (baseType.Parent?.Argument ?? "Anonymous") + "-" + baseType.Argument + j;
                HashSet<string> types = [];
                foreach (var option in options)
                {
                    if (BuiltinTypeReference.IsBuiltin(option, out var t) && t is not null && t is not "enum")
                    {
                        types.Add(t);
                        continue;
                    }

                    if (option.Children.Length == 0)
                    {
                        types.Add(MakeName(option.Argument));
                        continue;
                    }

                    i++;

                    var n = nameSource(Array.IndexOf(baseType.Children, option));
                    builder.AppendLine(HandleType(option, n));
                    types.Add(MakeName(n));
                }

                builder.AppendLine($$"""
                                     {{DescriptionString}}{{AttributeString}}
                                     public class {{name}}
                                     {
                                         {{Indent(string.Join("\n", types.Select(typeName => $"public {typeName}? {Capitalize(typeName).Split(':').Last()} {{ get; set; }}")))}}
                                         {{Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {typeName}({name} input) => input.{Capitalize(typeName).Split(':').Last()} ?? throw new InvalidOperationException(\"Union was not of effective type {typeName}\");")))}}
                                         {{Indent(string.Join("\n", types.Select(typeName => $"public static implicit operator {name}({typeName} input) => new {name}{{ {Capitalize(typeName).Split(':').Last()} = input }};")))}}
                                     }
                                     """);
                return builder.ToString();
            }
            default:
                return $$"""
                         {{DescriptionString}}{{AttributeString}}
                         public class {{name}}
                         {
                            public {{type}} Value { get; set; }
                            public static implicit operator {{type}}({{name}} instance) => instance.Value;
                            public static implicit operator {{name}}({{type}} value) => new {{name}}{ Value = value };
                         }
                         """;
        }
    }

    protected string InterfaceName(Base b)
    {
        return InterfaceName(b.Argument);
    }

    protected string InterfaceName(string arg)
    {
        var components = MakeName(arg).Split(':');
        components[components.Length - 1] = "I" + components[components.Length - 1];
        return string.Join(":", components);
    }

    protected string SingleLine(string multiline)
    {
        return string.Join(" ", multiline.Split('\n').Select(s => s.Trim()));
    }
}