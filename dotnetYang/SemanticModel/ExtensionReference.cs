using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class ExtensionReference : Statement
{
    public ExtensionReference(YangStatement statement) : base(statement, false)
    {
        SourceModulePrefix = statement.Prefix;
        ExtensionName = statement.Keyword;
        Argument = statement.Argument?.ToString() ?? string.Empty;
    }

    public string ExtensionName { get; }

    public string SourceModulePrefix { get; }

    public string ClassName
    {
        get
        {
            var classNameSource = Argument.Contains('/') ? ExtensionName + StableHash.Compute(Argument) : Argument;
            return MakeName(classNameSource) + "Extension";
        }
    }

    public override string ToCode()
    {
        var sourceModule = this.FindSourceFor(SourceModulePrefix);
        var source = sourceModule?.Extensions.FirstOrDefault(e => e.Argument == ExtensionName);
        if (source is null)
        {
            throw new SemanticError(
                $"Could not find source extension for {SourceModulePrefix}:{ExtensionName} in module {sourceModule}",
                Source);
        }

        var children = Children.Select(c => c.ToCode()).ToArray();
        var inheritance = string.IsNullOrWhiteSpace(SourceModulePrefix)
            ? MakeName(ExtensionName)
            : SourceModulePrefix + ':' + MakeName(ExtensionName);
        var constructor = source.TryGetChild<Argument>(out _)
            ? $$"""
                public {{ClassName}}() : base("{{SingleLine(Argument).Replace("\n", "\\\n")}}")
                    {
                    }
                    
                """
            : string.Empty;

        return $$"""
                 public {{ClassName}}? {{ClassName}}Value { get; }
                 {{DescriptionString}}{{AttributeString}}
                 public class {{ClassName}} : {{inheritance}}, YangSupport.IYangNode
                 {
                     YangSupport.IYangNode? YangSupport.IYangNode.YangParent => null;
                     public object? GetChild(string yangName) => null;
                     {{constructor}}{{Indent(string.Join("\n", children))}}
                 }
                 """;
    }
}