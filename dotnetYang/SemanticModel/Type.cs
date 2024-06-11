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
        new ChildRule(Base.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Keyword, Cardinality.ZeroOrMore),
    ];

    public override string ToCode()
    {
        return string.Empty;
    }

    public string? Definition
    {
        get
        {
            if (m_definition != null) return m_definition;
            if (!BuiltinTypeReference.IsBuiltin(this, out var typeName, out var definition))
            {
                if (this.FindReference<IStatement>(Argument) != null) return null;
                m_definition = BuiltinTypeReference.DefaultPattern(this, [], [], TypeName(this), Name!);
                return m_definition;
            }

            m_name = typeName;
            m_definition = definition;
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

            var prefix = Argument.Prefix(out var value);
            var reference = this.FindReference<IStatement>(Argument);
            if (reference is TypeDefinition def)
            {
                if (prefix.Contains('.')) return prefix + MakeName(def.Argument);
                else return prefix + ":" + MakeName(def.Argument);
            }

            if (reference != null && !BuiltinTypeReference.IsBuiltinKeyword(Argument)) return MakeName(Argument);
            m_name = BuiltinTypeReference.TypeName(this);
            return m_name;
        }
    }
}