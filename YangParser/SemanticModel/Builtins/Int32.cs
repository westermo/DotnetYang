namespace YangParser.SemanticModel.Builtins;

public class Int32() : BuiltinType("int32", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("int", null);
    var name = BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "int", name));
});