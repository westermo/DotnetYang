namespace YangParser.SemanticModel.Builtins;

public class Uint32() : BuiltinType("uint32", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("uint", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "uint", name));
});