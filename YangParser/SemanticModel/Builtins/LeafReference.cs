using System.Linq;

namespace YangParser.SemanticModel.Builtins;

public class LeafReference() : BuiltinType("leafref", (statement) =>
{
    var path = (Path)statement.Children.First(c => c is Path);
    var name =  BuiltinTypeReference.TypeName(statement);
    var definition = $$"""
                       {{statement.DescriptionString}}{{statement.AttributeString}}
                       public class {{name}}() : InstanceIdentifier("{{path.Argument}}")
                       {
                            public new static {{name}} Parse(string value) => new();
                       }
                       
                       """;
    return (name, definition);
});