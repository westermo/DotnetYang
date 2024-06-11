using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class String() : BuiltinType("string", statement =>
{
    var hasPattern = statement.TryGetChild<Pattern>(out var pattern);
    var hasLength = statement.TryGetChild<Length>(out var length);
    if (hasPattern || hasLength)
    {
        var name = BuiltinTypeReference.TypeName(statement);
        List<string> staticFields = new();
        List<string> constructorStatements = new();
        if (hasPattern)
        {
            foreach (var child in pattern!.Children)
            {
                child.ToCode();
            }

            staticFields.Add(pattern.GetDeclaration());
            constructorStatements.Add(pattern.GetConstructorValidation());
        }

        if (hasLength)
        {
            constructorStatements.Add(length!.GetConstructorValidation());
        }

        return (name, BuiltinTypeReference.DefaultPattern(statement, staticFields, constructorStatements, "string", name));
    }

    return ("string", null);
});