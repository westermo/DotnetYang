using System.Collections.Generic;

namespace Yang.Compiler;

public class BuiltInType(string name, string? cSharpName = null) : IType
{
    public string CSharpName => cSharpName ?? Name;

    public string Name { get; } = name;
    public IToken? Parent => null;
}

public static class BuiltIns
{
    public static List<IType> BuiltInTypes =
    [
        new BuiltInType("binary", "string"),
        new BuiltInType("binary", "string"),
        new BuiltInType("bits", "string"),
        new BuiltInType("boolean", "bool"),
        new BuiltInType("decimal", "double"),
        new BuiltInType("empty", string.Empty),
        new BuiltInType("enumeration", "string[]"),
        new BuiltInType("identityref", string.Empty),
        new BuiltInType("instance-identifier", string.Empty),
        new BuiltInType("int8", "sbyte"),
        new BuiltInType("int16", "short"),
        new BuiltInType("int32", "int"),
        new BuiltInType("int64", "long"),
        new BuiltInType("leafref", string.Empty),
        new BuiltInType("string", "string"),
        new BuiltInType("uint8", "byte"),
        new BuiltInType("uint16", "ushort"),
        new BuiltInType("uint32", "uint"),
        new BuiltInType("uint64", "ulong"),
        new BuiltInType("union", "string")
    ];
}