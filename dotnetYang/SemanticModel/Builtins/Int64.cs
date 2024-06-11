namespace YangParser.SemanticModel.Builtins;

public class Int64() : BuiltinType("int64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("long", null);
    var name = BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "long", name));
});