namespace YangParser.SemanticModel.Builtins;

public class Empty() : BuiltinType("empty", (s) => ("object", null));