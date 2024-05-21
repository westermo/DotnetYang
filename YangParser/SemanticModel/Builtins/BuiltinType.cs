using System;
using System.Collections.Generic;

namespace YangParser.SemanticModel.Builtins;

public static class BuiltinTypeReference
{
    private static readonly List<BuiltinType> m_builtIns =
    [
        new Binary(),
        new Bits(),
        new Boolean(),
        new Decimal64(),
        new Empty(),
        new Enumeration(),
        new IdentityReference(),
        new InstanceIdentifier(),
        new Int8(),
        new Int16(),
        new Int32(),
        new Int64(),
        new LeafReference(),
        new String(),
        new Uint8(),
        new Uint16(),
        new Uint32(),
        new Uint64(),
        new Union()
    ];

    public static bool IsBuiltin(Type type, out string? cSharpType)
    {
        cSharpType = null;
        foreach (var builtin in m_builtIns)
        {
            if (builtin.Name != type.Argument) continue;
            cSharpType = builtin.CorrespondingCSharpType(type);
            return true;
        }

        return false;
    }
}

public class BuiltinType(string name, Func<IStatement, string> correspondingCSharpType)
{
    public string Name => name;
    public Func<IStatement, string> CorrespondingCSharpType => correspondingCSharpType;
}

public class Binary() : BuiltinType("binary", (_) => "byte[]");

public class Bits() : BuiltinType("bits", (_) => null);

public class Boolean() : BuiltinType("boolean", (s) => "bool");

public class Decimal64() : BuiltinType("decimal64", (s) => "double");

public class Empty() : BuiltinType("empty", (s) => null);

public class Enumeration() : BuiltinType("enumeration", (s) => "enum");

public class IdentityReference() : BuiltinType("identityref", (s) => null);

public class InstanceIdentifier() : BuiltinType("instance-identifier", (s) => null);

public class Int8() : BuiltinType("int8", (s) => "sbyte");

public class Int16() : BuiltinType("int16", (s) => "short");

public class Int32() : BuiltinType("int32", (s) => "int");

public class Int64() : BuiltinType("int64", (s) => "long");

public class LeafReference() : BuiltinType("leafref", (s) => null);

public class String() : BuiltinType("string", (s) => "string");

public class Uint8() : BuiltinType("uint8", (s) => "byte");

public class Uint16() : BuiltinType("uint16", (s) => "ushort");

public class Uint32() : BuiltinType("uint32", (s) => "uint");

public class Uint64() : BuiltinType("uint64", (s) => "ulong");

public class Union() : BuiltinType("union", (s) => null);