using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YangParser.Parser;
using YangParser.SemanticModel;

namespace YangParser.Generator;

[Generator]
public class YangGenerator : IIncrementalGenerator
{
    private readonly struct ResultOrException<T>
    {
        public ResultOrException(Exception exception)
        {
            Exception = exception;
            Success = false;
        }

        public ResultOrException(T result)
        {
            Result = result;
            Success = true;
        }

        public readonly bool Success;
        public readonly Exception? Exception;
        public readonly T? Result;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(AddAttributesClass);
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var parsed = yangFiles.Select((p, _) => Parse(p));
        var model = parsed.Select((p, _) => MakeSemanticModel(p));
        context.RegisterSourceOutput(model.Collect(), MakeClasses);
    }

    private void MakeClasses(SourceProductionContext context, ImmutableArray<ResultOrException<IStatement>> models)
    {
        try
        {
            Dictionary<string, IStatement> topLevels = new();
            Dictionary<string, Module> modules = new();
            foreach (var model in models)
            {
                if (!model.Success)
                {
                    ReportDiagnostics(context, model);
                    continue;
                }

                var statement = model.Result!;
                switch (statement)
                {
                    case Module module:
                        topLevels[module.Argument] = module;
                        modules[module.Argument] = module;
                        break;
                    case Submodule submodule:
                        topLevels[submodule.Argument] = submodule;
                        break;
                }
            }


            //Replace Includes with their respective submodules
            IncludeSubmodules(context, modules, topLevels);
            var compilation = new CompilationUnit(modules.Values.ToArray());

            //Replace Uses by their respective groupings
            UnwrapUses(context, compilation);

            foreach (var module in compilation.Children)
            {
                context.AddSource(module.Argument + ".cs", module.ToCode());
            }
        }
        catch (Exception e)
        {
            ReportDiagnostics(context, new ResultOrException<IStatement>(e));
        }
    }

    private static void UnwrapUses(SourceProductionContext context, IStatement compilation)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            var usings = module.Unwrap().OfType<Uses>().ToArray();
            foreach (var use in usings)
            {
                if (use.IsUnderGrouping())
                {
                    continue;
                }

                try
                {
                    var grouping = use.GetGrouping();
                    var parent = use.Parent;
                    parent!.Replace(use, grouping.WithUse(use));
                    if (parent.Children.Contains(use))
                    {
                        throw new SemanticError(
                            $"'Failed to replace '{use.Argument}' in '{parent.GetType().Name} {parent.Argument}'",
                            module.Source, usings.Select(u => u.Source).ToArray());
                    }
                }
                catch (SemanticError error)
                {
                    ReportDiagnostics(context, new ResultOrException<IStatement>(error));
                }
            }
        }
    }

    private static void IncludeSubmodules(SourceProductionContext context, Dictionary<string, Module> modules,
        Dictionary<string, IStatement> topLevels)
    {
        foreach (var module in modules.Values)
        {
            var includes = module.Unwrap().OfType<Include>().ToArray();
            foreach (var include in includes)
            {
                if (!topLevels.TryGetValue(include.Argument, out var submodule))
                {
                    ReportDiagnostics(context,
                        new ResultOrException<IStatement>(new SemanticError(
                            $"Could not find a subModule with the key {include.Argument}",
                            include.Source)));
                    continue;
                }

                if (submodule.TryGetChild<BelongsTo>(out var belongsTo))
                {
                    if (module.Argument != belongsTo?.Argument)
                    {
                        ReportDiagnostics(context,
                            new ResultOrException<IStatement>(new SemanticError(
                                $"Include of module {submodule.Argument} that does not belong to module {module.Argument} (belongs to {belongsTo?.Argument})",
                                include.Source)));
                    }
                }

                include.Parent?.Replace(include, submodule.Children);
            }
        }
    }

    private static void ReportDiagnostics(SourceProductionContext context, ResultOrException<IStatement> model)
    {
        switch (model.Exception)
        {
            case SemanticError e:
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        SemanticError,
                        e.Location,
                        e.AdditionalLocations,
                        e.Message
                    )
                );
                break;
            case SyntaxError e:
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ParsingError,
                        Location.Create(
                            e.SourceRef,
                            new TextSpan(e.Token.Position.Offset, e.Token.Length),
                            new LinePositionSpan(
                                new LinePosition(e.Token.Position.Line, e.Token.Position.Column),
                                new LinePosition(e.Token.Position.Line,
                                    e.Token.Position.Column + e.Token.Length)
                            )
                        ),
                        e.Message
                    )
                );
                break;
            default:
                try
                {
                    var source = model.Exception!.Data["source"] as YangStatement;
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            SemanticError,
                            Location.Create(source!.Metadata.Source, new TextSpan(), new LinePositionSpan()),
                            model.Exception.Message + model.Exception.StackTrace
                        )
                    );
                }
                catch
                {
                    try
                    {
                        var textFile = model.Exception!.Data["textFile"] as AdditionalText;
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                ParsingError,
                                Location.Create(textFile!.Path, new TextSpan(), new LinePositionSpan()),
                                model.Exception.Message + model.Exception.StackTrace
                            )
                        );
                    }
                    catch
                    {
                        //Just report a nowhere-diagnostic
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                ParsingError,
                                Location.None,
                                model.Exception?.Message + model.Exception?.StackTrace
                            )
                        );
                    }
                }

                break;
        }
    }

    private static readonly DiagnosticDescriptor ParsingError = new DiagnosticDescriptor("YANG0001", "Parsing Error",
        "Parsing Error: {0}", "Parser", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor SemanticError = new DiagnosticDescriptor("YANG0002", "Semantic Error",
        "Semantic Error: {0}", "SemanticModel", DiagnosticSeverity.Error, true);

// private void MakeClasses(SourceProductionContext context, AdditionalText text)
// {
//     if (!Parse(context, text, out var parsed)) return;
//     if (!MakeSemanticModel(context, parsed, out var statement)) return;
//     if (statement is not Module module) return;
//     context.AddSource(module.Filename, module.ToCode());
// }

    private static ResultOrException<IStatement> MakeSemanticModel(ResultOrException<YangStatement> statement)

    {
        if (!statement.Success) return new ResultOrException<IStatement>(statement.Exception!);
        try
        {
            return new ResultOrException<IStatement>(StatementFactory.Create(statement.Result!));
        }
        catch (Exception ex)
        {
            ex.Data["source"] = statement.Result;
            return new ResultOrException<IStatement>(ex);
        }
    }

    private static ResultOrException<YangStatement> Parse(AdditionalText text)
    {
        try
        {
            return new ResultOrException<YangStatement>(Parser.Parser.Parse(text.Path, text.GetText()!.ToString()));
        }
        catch (Exception ex)
        {
            ex.Data["textFile"] = text;
            return new ResultOrException<YangStatement>(ex);
        }
    }


    private void AddAttributesClass(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("Yang.Attributes.cs", """
                                                using System;
                                                namespace Yang.Attributes;
                                                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                                                public class RevisionAttribute(string date) : Attribute
                                                {
                                                    public string Date { get; } = date;
                                                }

                                                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                                                public class PresenceAttribute(string meaning) : Attribute
                                                {
                                                    public string Meaning { get; } = meaning;
                                                }
                                                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                                                public class ProvidesFeatureAttribute(string flag) : Attribute
                                                {
                                                    public string FeatureFlag { get; } = flag;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                                                public class IfFeatureAttribute(string flag) : Attribute
                                                {
                                                    public string FeatureFlag { get; } = flag;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                                                public class ReferenceAttribute(string reference) : Attribute
                                                {
                                                    public string Reference { get; } = reference;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                                                public class WhenAttribute(string xPath) : Attribute
                                                {
                                                    public string XPath { get; } = xPath;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
                                                public class TargetAttribute(string xPath) : Attribute
                                                {
                                                    public string XPath { get; } = xPath;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                public class KeyAttribute(string value) : Attribute
                                                {
                                                    public string Value { get; } = value;
                                                }
                                                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                                                public class MinElements(int value) : Attribute
                                                {
                                                    public int Value { get; } = value;
                                                }
                                                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                                                public class MaxElements(int value) : Attribute
                                                {
                                                    public int Value { get; } = value;
                                                }
                                                [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                public class NotConfigurationData : Attribute;

                                                public interface IInstanceIdentifier
                                                {
                                                    public string Path { get; }
                                                }
                                                """);
    }
}