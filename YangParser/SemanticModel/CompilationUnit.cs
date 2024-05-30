using System;
using System.Collections.Generic;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class CompilationUnit : Statement
{
    public CompilationUnit(Module[] modules, string Namespace = "Somewhere") : base(new YangStatement(String.Empty,
        string.Empty, [],
        new Metadata(string.Empty, new Parser.Position(), 0)))
    {
        this.Namespace = Namespace;
        Children = modules;
    }

    public string Namespace { get; set; }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Module.Keyword, Cardinality.ZeroOrMore),
    ];

    public override string ToCode()
    {
        var members = new List<string>();
        foreach (var module in Children.OfType<Module>())
        {
            var typeName = module.Namespace.Substring(0, module.Namespace.Length - 1);
            var memberName = MakeName(module.Argument);
            members.Add($"public {typeName}? {memberName} {{ get; set; }}");
        }

        return $$"""
                 using System;
                 using System.Xml;
                 using Yang.Attributes;
                 namespace {{Namespace}};
                 ///<summary>
                 ///Configuration root object for {{Namespace}} based on provided .yang modules
                 ///</summary>{{AttributeString}}
                 public class Configuration
                 {
                     {{Indent(string.Join("\n", members))}}
                 }
                 """;
    }
}