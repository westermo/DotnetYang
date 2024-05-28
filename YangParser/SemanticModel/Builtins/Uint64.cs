namespace YangParser.SemanticModel.Builtins;

public class Uint64() : BuiltinType("uint64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("ulong", null);
    var name = BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "ulong", name));
});