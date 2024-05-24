using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class LeafReference() : BuiltinType("leafref", (s) =>
{
    var path = (Path)s.Children.First(c => c is Path);
    var name = Statement.MakeName(s.Parent!.Argument);
    var definition = $$"""
                       {{s.DescriptionString}}{{s.AttributeString}}
                       public class {{name}}() : InstanceIdentifier("{{path.Argument}}");
                       """;
    return (name, definition);
});