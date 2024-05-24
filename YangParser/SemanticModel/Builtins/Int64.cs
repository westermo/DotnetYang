namespace YangParser.SemanticModel.Builtins;

public class Int64() : BuiltinType("int64", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("long", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "long", name));
});