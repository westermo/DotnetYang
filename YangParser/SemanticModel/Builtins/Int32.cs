namespace YangParser.SemanticModel.Builtins;

public class Int32() : BuiltinType("int32", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("int", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "int", name));
});