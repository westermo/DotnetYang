namespace YangParser.SemanticModel.Builtins;

public class Uint16() : BuiltinType("uint16", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("ushort", null);
    var name =  BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "ushort", name));
});