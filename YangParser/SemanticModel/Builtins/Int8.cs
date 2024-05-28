namespace YangParser.SemanticModel.Builtins;

public class Int8() : BuiltinType("int8", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("sbyte", null);
    var name = BuiltinTypeReference.TypeName(statement);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "sbyte", name));
});