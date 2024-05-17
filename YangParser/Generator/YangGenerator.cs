using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YangParser.Parser;
using YangParser.SemanticModel;

namespace YangParser.Generator;

[Generator]
public class YangGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var parsed = yangFiles.Select((p, _) => Parser.Parser.Parse(p.Path, p.GetText()!.ToString()));
        var model = parsed.Select((p, _) => StatementFactory.Create(p));
        context.RegisterSourceOutput(yangFiles, MakeClasses);
    }

    private static readonly DiagnosticDescriptor ParsingError = new DiagnosticDescriptor("YANG0001", "Parsing Error",
        "Parsing Error: {0}", "Parser", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor SemanticError = new DiagnosticDescriptor("YANG0002", "Semantic Error",
        "Semantic Error: {0}", "SemanticModel", DiagnosticSeverity.Error, true);

    private void MakeClasses(SourceProductionContext context, AdditionalText text)
    {
        if (!Parse(context, text, out var parsed)) return;
        if (!MakeSemanticModel(context, parsed, out var statement)) return;
        if (statement is Module module)
        {
            var usings = new Dictionary<string, string>();
            string ns = MakeNamespace(module.Argument);
            var functions = new List<string>();
            foreach (var child in module.Children)
            {
                if (child is Import import)
                {
                    var use = MakeNamespace(import.Argument);
                    if (child.Children.FirstOrDefault(x => x is Prefix) is Prefix prefix)
                    {
                        usings[prefix.Argument] = use;
                    }
                    else
                    {
                        usings[use] = use;
                    }
                }

                if (child is Prefix modulePrefix)
                {
                    usings[modulePrefix.Argument] = string.Empty;
                }
            }

            WalkTree(context, ns, statement, functions);
            context.AddSource(ns, $$"""
                                    using System;
                                    using System.Xml.Linq;
                                    namespace {{ns}};
                                    public static class RemoteProcedureCalls
                                    {
                                        public const string Namespace = "{{module.XmlNamespace.Argument}}";
                                        {{string.Join("\n", functions)}}
                                    }
                                    """);
        }
    }

    private static bool MakeSemanticModel(SourceProductionContext context, YangStatement statement,
        out IStatement model)

    {
        try
        {
            model = StatementFactory.Create(statement);
        }
        catch (SemanticError e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    SemanticError,
                    e.Location,
                    e.Message
                )
            );
            model = null!;
            return false;
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    SemanticError,
                    Location.Create(statement.Metadata.Source, new TextSpan(), new LinePositionSpan()),
                    e.Message + e.StackTrace
                )
            );
            model = null!;
            return false;
        }

        return true;
    }

    private static bool Parse(SourceProductionContext context, AdditionalText text, out YangStatement parsed)
    {
        try
        {
            parsed = Parser.Parser.Parse(text.Path, text.GetText()!.ToString());
        }
        catch (SyntaxError e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ParsingError,
                    Location.Create(
                        text.Path,
                        new TextSpan(e.Token.Position.Offset, e.Token.Length),
                        new LinePositionSpan(
                            new LinePosition(e.Token.Position.Line, e.Token.Position.Column),
                            new LinePosition(e.Token.Position.Line, e.Token.Position.Column + e.Token.Length)
                        )
                    ),
                    e.Message
                )
            );
            parsed = null!;
            return false;
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ParsingError,
                    Location.Create(text.Path, new TextSpan(), new LinePositionSpan()),
                    e.Message
                )
            );
            parsed = null!;
            return false;
        }

        return true;
    }

    private void WalkTree(SourceProductionContext context, string Namespace, IStatement statement,
        List<string> functions)
    {
        if (statement.Children.FirstOrDefault(x => x is Uses) is Uses uses)
        {
            UnwrapUses(uses);
        }

        foreach (var child in statement.Children)
        {
            if (child is ICommentSource commentSource)
            {
                if (commentSource.Parent is ICommentable commentable)
                {
                    commentable.Comments.Add("///" + commentSource.Argument);
                }

                continue;
            }

            if (child is IClassSource classSource)
            {
                MakeClass(context, Namespace, classSource, functions);
                continue;
            }

            if (child is IFunctionSource functionSource)
            {
                functions.Add(MakeFunction(functionSource));
            }
        }
    }

    private void UnwrapUses(Uses uses)
    {
        var index = Array.IndexOf(uses.Parent!.Children, uses);
    }

    private string MakeFunction(IFunctionSource functionSource)
    {
        return $"public static XElement {MakeClassName(functionSource.Argument)}() => new XElement(\"dummy\");";
    }

    private void MakeClass(SourceProductionContext context, string Namespace, IClassSource classSource,
        List<string> functions)
    {
        var name = MakeClassName(classSource.Argument);
        var childNamespaces = Namespace + "." + name;
        WalkTree(context, childNamespaces, classSource, functions);
        context.AddSource(childNamespaces, $$"""
                                             using System;

                                             namespace {{Namespace}};
                                             /// <summary>
                                             {{string.Join("\n", classSource.Comments)}}
                                             /// </summary>
                                             public class {{name}}
                                             {
                                               public string Dummy;
                                             }
                                             """);
    }

    private string Capitalize(string section)
    {
        var first = section[0];
        var rest = section.Substring(1, section.Length - 1);
        return char.ToUpperInvariant(first) + rest;
    }

    private string MakeNamespace(string argument)
    {
        var output = new StringBuilder(argument.Length);
        foreach (var section in argument.Split('-'))
        {
            output.Append(Capitalize(section));
            if (output.Length < argument.Length) output.Append('.');
        }

        return output.ToString();
    }

    private string MakeClassName(string argument)
    {
        var output = new StringBuilder(argument.Length);
        foreach (var section in argument.Split('-'))
        {
            output.Append(Capitalize(section));
        }

        return output.ToString();
    }
}