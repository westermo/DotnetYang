namespace YangParser.SemanticModel.Builtins;

public class Int16() : BuiltinType("int16", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("short", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name,
        BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "short", name));
});