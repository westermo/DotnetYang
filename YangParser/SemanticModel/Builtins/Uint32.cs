namespace YangParser.SemanticModel.Builtins;

public class Uint32() : BuiltinType("uint32", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("uint", null);
    var name =  BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "uint", name));
});