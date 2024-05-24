namespace YangParser.SemanticModel.Builtins;

public class Decimal64() : BuiltinType("decimal64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("double", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "long", name));
});