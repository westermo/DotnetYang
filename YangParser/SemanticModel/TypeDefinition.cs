using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class TypeDefinition : Statement, IFunctionSource
{
    private readonly YangStatement m_source;

    public TypeDefinition(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        BaseType = Children.OfType<Type>().First();
        m_source = statement;
    }

    public Type BaseType { get; }

    public const string Keyword = "typedef";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Description.Keyword),
        new ChildRule(Reference.Keyword),
        new ChildRule(Status.Keyword),
        new ChildRule(Type.Keyword, Cardinality.Required),
        new ChildRule(Units.Keyword),
    ];

    public List<string> Comments { get; } = new();

    public override string ToCode()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        return HandleType(BaseType, Argument);
    }

    private string HandleType(Type Input, string typename)
    {
        var name = MakeName(typename);
        if (BuiltinTypeReference.IsBuiltin(Input, out var type))
        {
            return HandleSpecialTypes(Input, name, type);
        }

        return $$"""
                 {{DescriptionString}}
                 {{AttributeString}}
                 public class {{MakeName(Argument)}} : {{TypeName(Input)}} //{{Input.Argument}}
                 {
                 }
                 """;
    }

    private string HandleSpecialTypes(Type baseType, string name, string? type)
    {
        switch (baseType.Argument)
        {
            case "identityref":
            {
                var inherits = baseType.Children.OfType<Base>().Select(b => "I" + MakeName(b.Argument)).ToArray();
                var inheritance = inherits.Length == 0 ? string.Empty : " : " + string.Join(", ", inherits);
                return $"""
                        {DescriptionString}
                        {AttributeString}
                        public class {name}{inheritance};
                        """;
            }
            case "instance-identifier":
                return $$"""
                         {{DescriptionString}}
                         {{AttributeString}}
                         public class {{name}} : IInstanceIdentifier
                         {
                             public required string Path { get; set; }
                         }
                         """;
            case "leafref":
                var path = baseType.Children.First(c => c is Path) as Path;

                return $$"""
                         {{DescriptionString}}
                         {{AttributeString}}
                         public class {{name}} : IInstanceIdentifier
                         {
                             public string Path => "{{path.Argument}}";
                         }
                         """;
            case "enumeration":
            {
                var enums = baseType.Children.OfType<Enum>();
                var stringified = enums.Select(e => e.ToCode());
                return $$"""
                         /*
                         {{Print(baseType)}}
                         */
                         {{DescriptionString}}
                         {{AttributeString}}
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
                foreach (var option in options)
                {
                    i++;
                    builder.AppendLine(Indent(HandleType(option, baseType.Argument + i)));
                }

                builder.AppendLine($$"""
                                     {{DescriptionString}}
                                     {{AttributeString}}
                                     public class {{name}}
                                     {
                                         {{Indent(string.Join(",\n", options.Select((o, i) => $"public {baseType.Argument + (i + 1)} Option{i + 1} {{ get; set; }}")))}}
                                         
                                         {{Indent(string.Join(",\n", options.Select((o, i) => $"public static implicit operator {baseType.Argument + (i + 1)}({name} input) => input.Option{i + 1};")))}}
                                         
                                         {{Indent(string.Join(",\n", options.Select((o, i) => $"public static implicit operator {name}({baseType.Argument + (i + 1)} input) => new {name}{{ Option{i + 1} = input }};")))}}
                                     }
                                     """);
                return builder.ToString();
            }
            default:
                return $$"""
                         /*
                         {{Print(baseType)}}
                         */
                         {{DescriptionString}}
                         {{AttributeString}}
                         public class {{name}}
                         {
                            public {{type}} Value { get; set; }
                            public static implicit operator {{type}}({{name}} instance) => instance.Value;
                            public static implicit operator {{name}}({{type}} value) => new {{name}}{ Value = value };
                         }
                         """;
        }
    }
}