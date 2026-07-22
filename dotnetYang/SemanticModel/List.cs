using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class List : Statement, IClassSource, IXMLWriteValue, IXMLReadValue
{
    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(AnyData.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(AnyXml.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Action.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Choice.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Config.Keyword),
        new ChildRule(Container.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Description.Keyword),
        new ChildRule(Grouping.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(FeatureFlag.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Key.Keyword),
        new ChildRule(Leaf.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(LeafList.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Notification.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(MinElements.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(OrderedBy.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(TypeDefinition.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Unique.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Uses.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(When.Keyword)
    ];

    public List(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "list";
    public List<string> Comments { get; } = new();

    /// <summary>
    /// Whether this list has a key statement (i.e., should use YangList).
    /// </summary>
    public bool HasKey => this.TryGetChild<Key>(out _);

    /// <summary>
    /// Gets the key statement if present.
    /// </summary>
    public Key? GetKey() => this.TryGetChild<Key>(out var k) ? k : null;

    /// <summary>
    /// Resolves key leaf C# type names after children have been code-generated.
    /// Returns null if key fields cannot be resolved.
    /// </summary>
    private string[]? ResolveKeyTypes()
    {
        var key = GetKey();
        if (key == null) return null;

        var types = new List<string>();
        foreach (var fieldName in key.KeyPropertyNames)
        {
            var leafChild = Children.OfType<Leaf>()
                .FirstOrDefault(l => l.TargetName == fieldName);
            if (leafChild == null)
            {
                // Key field not found among direct leaf children - could be from uses/augment
                return null;
            }
            types.Add(leafChild.ClassName);
        }
        return types.ToArray();
    }

    /// <summary>
    /// Gets the C# key type string: single type for single keys, ValueTuple for composite keys.
    /// Returns null if key types cannot be resolved.
    /// </summary>
    public string? GetKeyTypeString()
    {
        if (_keyTypeString != null) return _keyTypeString;
        var types = ResolveKeyTypes();
        if (types == null || types.Length == 0) return null;
        _keyTypeString = types.Length == 1 ? types[0] : $"({string.Join(", ", types)})";
        return _keyTypeString;
    }

    private string? _keyTypeString;

    /// <summary>
    /// Gets the key extractor lambda expression for YangList construction.
    /// </summary>
    private string? GetKeyExtractorLambda()
    {
        var key = GetKey();
        if (key == null) return null;
        var propNames = key.KeyPropertyNames;
        if (propNames.Length == 1)
        {
            return $"entry => entry.{propNames[0]}";
        }
        return $"entry => ({string.Join(", ", propNames.Select(p => $"entry.{p}"))})";
    }

    /// <summary>
    /// The full collection type string for this list's property.
    /// </summary>
    public string CollectionTypeString
    {
        get
        {
            var keyType = GetKeyTypeString();
            if (keyType != null)
            {
                return $"YangList<{keyType}, {ClassName}>";
            }
            return $"List<{ClassName}>";
        }
    }

    public override string ToCode()
    {
        var nodes = Children.Select(child => child.ToCode()).ToArray();
        var hasMinElements = this.TryGetChild<MinElements>(out var minEl) && minEl!.Value > 0;
        var nullable = hasMinElements && !Children.Any(c => c is When) ? string.Empty : "?";

        var keyType = GetKeyTypeString();
        var collectionType = CollectionTypeString;
        var key = GetKey();
        var parentName = ParentClassName;
        var classInterfaces = keyType != null
            ? $" : global::System.IEquatable<{ClassName}>, YangSupport.IYangNode"
            : " : YangSupport.IYangNode";
        var equalityMembers = (key != null && keyType != null) ? GenerateEqualityMembers(key) : string.Empty;

        string property;
        if (parentName is null)
        {
            property =
                $"\n{DescriptionString}\npublic{KeywordString}{collectionType}{nullable} {TargetName} {{ get; set; }}";
        }
        else if (keyType != null)
        {
            // YangList with OnAdded/OnRemoved hooks to wire Parent on entries
            property = $$"""

                         {{DescriptionString}}
                         private {{collectionType}}{{nullable}} _{{TargetName}};
                         public{{KeywordString}}{{collectionType}}{{nullable}} {{TargetName}}
                         {
                             get => _{{TargetName}};
                             set
                             {
                                 if (_{{TargetName}} is not null)
                                 {
                                     foreach (var __item in _{{TargetName}}) __item.YangParent = null;
                                     _{{TargetName}}.OnAdded = null;
                                     _{{TargetName}}.OnRemoved = null;
                                 }
                                 _{{TargetName}} = value;
                                 if (value is not null)
                                 {
                                     foreach (var __item in value) __item.YangParent = this;
                                     value.OnAdded = __item => __item.YangParent = this;
                                     value.OnRemoved = __item => __item.YangParent = null;
                                 }
                             }
                         }
                         """;
        }
        else
        {
            // Plain List<T> without key: emit setter that wires YangParent on existing items
            // (no add/remove hook available, callers must reassign or set YangParent manually).
            property = $$"""

                         {{DescriptionString}}
                         private {{collectionType}}{{nullable}} _{{TargetName}};
                         public{{KeywordString}}{{collectionType}}{{nullable}} {{TargetName}}
                         {
                             get => _{{TargetName}};
                             set
                             {
                                 if (_{{TargetName}} is not null)
                                 {
                                     foreach (var __item in _{{TargetName}}) __item.YangParent = null;
                                 }
                                 _{{TargetName}} = value;
                                 if (value is not null)
                                 {
                                     foreach (var __item in value) __item.YangParent = this;
                                 }
                             }
                         }
                         """;
        }

        var parentDecl = ParentPropertyDeclaration();
        var validate = global::YangParser.SemanticModel.XPath.ValidateEmitter.EmitValidateMethod(this);
        return $$"""
                 {{property}}
                 {{AttributeString}}
                 public class {{ClassName}}{{classInterfaces}}
                 {
                     {{Indent(parentDecl)}}
                     {{string.Join("\n\t", nodes.Select(Indent))}}
                     {{Indent(equalityMembers)}}
                     {{Indent(WriteFunction())}}
                     {{Indent(ReadFunction())}}
                     {{Indent(GetChildMethod())}}
                     {{Indent(validate)}}
                 }
                 """;
    }

    /// <summary>
    /// Generates Equals/GetHashCode/IEquatable implementations based on the list's key fields.
    /// Per RFC 7950 §7.8.2, a list entry is uniquely identified by its key.
    /// </summary>
    private string GenerateEqualityMembers(Key key)
    {
        var propNames = key.KeyPropertyNames;
        var equalsBody = string.Join(" && ",
            propNames.Select(p => $"global::System.Collections.Generic.EqualityComparer<object?>.Default.Equals(this.{p}, other.{p})"));
        var hashAdds = string.Join("\n        ",
            propNames.Select(p => $"hash.Add(this.{p});"));
        return $$"""
                 public bool Equals({{ClassName}}? other)
                 {
                     if (other is null) return false;
                     if (ReferenceEquals(this, other)) return true;
                     return {{equalsBody}};
                 }
                 public override bool Equals(object? obj) => Equals(obj as {{ClassName}});
                 public override int GetHashCode()
                 {
                     var hash = new global::System.HashCode();
                     {{hashAdds}}
                     return hash.ToHashCode();
                 }
                 """;
    }

    public string TargetName => MakeName(Argument);

    public string ClassName => TargetName + "Entry";

    public string WriteCall =>
        $$"""
          if({{TargetName}} != null)
          {
              foreach(var element in {{TargetName}})
              {
                  await element!.WriteXMLAsync(writer);
              }
          }
          """;

    public string ParseCall
    {
        get
        {
            var keyType = GetKeyTypeString();
            var keyExtractor = GetKeyExtractorLambda();
            var isUserOrdered = this.TryGetChild<OrderedBy>(out var ob) && ob!.IsUserOrdered;

            if (keyType != null && keyExtractor != null)
            {
                if (isUserOrdered)
                {
                    // ordered-by user: support yang:insert attribute for positional insertion
                    return $$"""
                             _{{TargetName}} ??= new YangList<{{keyType}}, {{ClassName}}>({{keyExtractor}});
                             var _{{TargetName}}Insert = reader.GetAttribute("insert", "urn:ietf:params:xml:ns:yang:1");
                             var _{{TargetName}}Element = await {{ClassName}}.ParseAsync(reader);
                             if (_{{TargetName}}Insert == "first")
                             {
                                 _{{TargetName}}.Insert(0, _{{TargetName}}Element);
                             }
                             else
                             {
                                 _{{TargetName}}.Add(_{{TargetName}}Element);
                             }
                             """;
                }

                return $$"""
                         _{{TargetName}} ??= new YangList<{{keyType}}, {{ClassName}}>({{keyExtractor}});
                         var _{{TargetName}}Element = await {{ClassName}}.ParseAsync(reader);
                         _{{TargetName}}.Add(_{{TargetName}}Element);
                         """;
            }
            return $$"""
                    _{{TargetName}} ??= new List<{{ClassName}}>();
                    var _{{TargetName}}Element = await {{ClassName}}.ParseAsync(reader);
                    _{{TargetName}}.Add(_{{TargetName}}Element);
                    """;
        }
    }
}