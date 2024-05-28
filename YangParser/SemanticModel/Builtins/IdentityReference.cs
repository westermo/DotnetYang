using System.Linq;
using YangParser.Generator;

namespace YangParser.SemanticModel.Builtins;

public class IdentityReference() : BuiltinType("identityref", statement =>
{
    var inherits = statement.Children.OfType<Base>().Select(x => '"' + x.Argument + '"').ToArray();
    var name = BuiltinTypeReference.TypeName(statement);
    var definition = $$"""
                      {{statement.DescriptionString}}{{statement.AttributeString}}
                      public class {{name}}
                      {
                        public string Value { get; }
                        public static string[] Bases = [{{string.Join(", ", inherits)}}];
                        public {{name}}(string input) => Value = input;
                        public static implicit operator string({{name}} input) => input.Value;
                        public static implicit operator {{name}}(string input) => new(input);
                      }
                      """;
    return (name, definition);
});