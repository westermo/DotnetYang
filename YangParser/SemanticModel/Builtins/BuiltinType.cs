using System;

namespace YangParser.SemanticModel.Builtins;

public class BuiltinType(string name, Func<IStatement, (string, string?)> correspondingCSharpType)
{
    public string Name => name;
    public Func<IStatement, (string, string?)> CorrespondingCSharpType => correspondingCSharpType;
}