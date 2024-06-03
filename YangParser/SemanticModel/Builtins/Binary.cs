using System.Collections.Generic;

namespace YangParser.SemanticModel.Builtins;

public class Binary() : BuiltinType("binary", statement =>
{
    var hasLength = statement.TryGetChild<Length>(out var length);
    List<string> staticFields = new();
    var typeName = BuiltinTypeReference.TypeName(statement);
    List<string> constructorStatements = new();
    if (hasLength)
    {
        constructorStatements.Add(length!.GetConstructorValidation());
    }

    return (typeName, $$"""
                        {{statement.DescriptionString}}{{statement.AttributeString}}
                        public class {{typeName}}
                        {
                            {{Statement.Indent(string.Join("\n", staticFields))}}
                            public byte[] WrittenValue { get; }
                            public static implicit operator byte[]?({{typeName}}? input) => input?.WrittenValue;
                            public static implicit operator {{typeName}}(byte[] input) => new {{typeName}}(input);
                            public {{typeName}}(byte[] input)
                            {
                                {{Statement.Indent(Statement.Indent(string.Join("\n", constructorStatements)))}}
                                WrittenValue = input;
                            }
                            public override string ToString()
                            {
                               return Convert.ToBase64String(WrittenValue);
                            }
                            public static {{typeName}} Parse(string base64)
                            {
                                return new {{typeName}}(Convert.FromBase64String(base64));
                            }
                            
                        }
                        """);
});