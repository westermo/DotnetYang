namespace YangParser.SemanticModel.Builtins;

public class BuiltinType(string name, string? correspondingCSharpType)
{
    public string Name => name;
    public string? CorrespondingCSharpType => correspondingCSharpType;
}

public class Binary() : BuiltinType("binary", "byte[]");

public class Bits() : BuiltinType("bits", null);

public class Boolean() : BuiltinType("boolean", "bool");

public class Decimal64() : BuiltinType("decimal64", "double");

public class Empty() : BuiltinType("empty", null);

public class Enumeration() : BuiltinType("enumeration", "enum");

public class IdentityReference() : BuiltinType("identityref", null);

public class InstanceIdentifier() : BuiltinType("instance-identifier", null);

public class Int8() : BuiltinType("int8", "sbyte");

public class Int16() : BuiltinType("int16", "short");

public class Int32() : BuiltinType("int32", "int");

public class Int64() : BuiltinType("int64", "long");

public class LeafReference() : BuiltinType("leafref", null);

public class String() : BuiltinType("string", "string");

public class Uint8() : BuiltinType("uint8", "byte");

public class Uint16() : BuiltinType("uint16", "ushort");

public class Uint32() : BuiltinType("uint32", "uint");

public class Uint64() : BuiltinType("uint64", "ulong");

public class Union() : BuiltinType("union", null);