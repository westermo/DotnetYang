namespace YangParser.SemanticModel.Builtins;

public class Uint64() : BuiltinType("uint64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("ulong", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "ulong", name));
});