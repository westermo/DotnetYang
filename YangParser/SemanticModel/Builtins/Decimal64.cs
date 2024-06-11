namespace YangParser.SemanticModel.Builtins;

public class Decimal64() : BuiltinType("decimal64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("double", null);
    var name = BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "double", name));
});