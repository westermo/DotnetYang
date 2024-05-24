using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class String() : BuiltinType("string", s =>
{
    var hasPattern = s.TryGetChild<Pattern>(out var pattern);
    var hasLength = s.TryGetChild<Length>(out var length);
    if (hasPattern || hasLength)
    {
        var name = Statement.MakeName(s.Parent!.Argument);
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

        return (name, BuiltinTypeReference.DefaultPattern(s, staticFields, constructorStatements, "string", name));
    }

    return ("string", null);
});