using System;
using System.Linq;
using YangParser.Parser;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public class Type : Statement
{
    private string? m_definition;
    private string? m_name;

    public Type(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "type";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Enum.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Bit.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Length.Keyword),
        new ChildRule(Pattern.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Path.Keyword),
        new ChildRule(Range.Keyword),
        new ChildRule(RequireInstance.Keyword),
        new ChildRule(FractionDigits.Keyword),
        new ChildRule(Base.Keyword),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
    ];

    public string? Definition
    {
        get
        {
            if (m_definition != null) return m_definition;
            if (!BuiltinTypeReference.IsBuiltin(this, out var typeName, out var definition))
            {
                if (this.FindReference(Argument) != null) return null;
                m_name = MakeName(Parent!.Argument) + "Type";
                m_definition = BuiltinTypeReference.DefaultPattern(this, [], [], TypeName(this), m_name);
                return m_definition;
            }

            m_definition = definition;
            m_name = typeName;
            return m_definition;
        }
    }

    public string? Name
    {
        get
        {
            if (m_name != null) return m_name;
            if (BuiltinTypeReference.IsBuiltin(this, out var typeName, out var definition))
            {
                m_definition = definition;
                m_name = typeName;
                return m_name;
            }

            var components = Argument.Split(':');
            string prefix = components.Length > 1 ? components[0] + ":" : string.Empty;
            var reference = this.FindReference(Argument);
            if (reference is TypeDefinition def)
            {
                return prefix + MakeName(def.Argument);
            }

            if (reference != null) return MakeName(Argument);
            m_name = MakeName(Parent!.Argument) + "Type";
            return m_name;
        }
    }
}