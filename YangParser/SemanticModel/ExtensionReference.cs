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
        var classNameSource = Argument.Contains('/') ? ExtensionName + Argument.GetHashCode() : Argument;
        if (source.TryGetChild<Argument>(out _))
        {
            return $$"""
                     public {{MakeName(classNameSource)}}Extension {{MakeName(classNameSource)}}ExtensionValue { get; }
                     {{DescriptionString}}{{AttributeString}}
                     public class {{MakeName(classNameSource)}}Extension : {{inheritance}}
                     {
                         public {{MakeName(classNameSource)}}Extension() : base("{{SingleLine(Argument).Replace("\n", "\\\n")}}")
                         {
                         }
                         {{Indent(string.Join("\n", children))}}
                     }
                     """;
        }

        return $$"""
                 public {{MakeName(classNameSource)}}Extension {{MakeName(classNameSource)}}ExtensionValue { get; }
                 {{DescriptionString}}{{AttributeString}}
                 public class {{MakeName(classNameSource)}}Extension : {{inheritance}}
                 {
                     {{Indent(string.Join("\n", children))}}
                 }
                 """;
    }
}