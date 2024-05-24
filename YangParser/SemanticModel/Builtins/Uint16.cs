namespace YangParser.SemanticModel.Builtins;

public class Uint16() : BuiltinType("uint16", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("ushort", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "ushort", name));
});