namespace YangParser.SemanticModel.Builtins;

public class Binary() : BuiltinType("binary", _ => ("byte[]", null));