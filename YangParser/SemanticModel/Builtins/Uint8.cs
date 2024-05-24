using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class Uint8() : BuiltinType("uint8", statement =>
{
    if (!statement.TryGetChild<Range>(out var range)) return ("byte", null);
    var name = Statement.MakeName(statement.Parent!.Argument);
    return (name, BuiltinTypeReference.DefaultPattern(statement, [], [range!.GetConstructorValidation()], "byte", name));
});