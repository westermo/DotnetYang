using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var parsed = yangFiles.Select((p, _) => Parse(p));
        var model = parsed.Select((p, _) => MakeSemanticModel(p));
        var combined = context.CompilationProvider.Combine(model.Collect());
        context.RegisterSourceOutput(combined, MakeClasses);
    }

    private void MakeClasses(SourceProductionContext context,
        (Compilation compilation, ImmutableArray<ResultOrException<IStatement>> models) data)
    {
        try
        {
            Dictionary<string, ITopLevelStatement> topLevels = new();
            Dictionary<string, Module> modules = new();
            foreach (var model in data.models)
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
                    default:
                        throw new SemanticError(
                            $"Unexpected top level statement of type {statement.GetType().Name} from {statement.Argument}",
                            statement.Source);
                }
            }

            //Replace Includes with their respective submodules
            IncludeSubmodules(context, modules, topLevels);
            var compilation = new CompilationUnit(modules.Values.ToArray(), data.compilation.AssemblyName!);
            //Replace Uses by their respective groupings
            UnwrapUses(context, compilation);
            InjectAugments(context, compilation);
            foreach (var module in compilation.Children.OfType<Module>())
            {
                try
                {
                    WriteFile(context, module.Filename, Clean(module.ToCode()));
                }
                catch (Exception e)
                {
                    WriteFile(context, module.Filename + ".errors",
                        $"#error Exception when generating code for {module.Filename}" + "\n/*\n" + e.Message + "\n" +
                        e.StackTrace + "\n*/");
                }
            }

            WriteFile(context, "Configuration.cs", compilation.ToCode());
        }
        catch (Exception e)
        {
            WriteFile(context, "errors",
                "#error General Exception" + "/*" + Statement.SingleLine(e.Message + "@" + e.StackTrace) + "*/");
        }

        foreach (var message in Log.Content)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    LogDescriptor,
                    Location.None,
                    message)
            );
        }

        Log.Clear();
    }

    private void InjectAugments(SourceProductionContext context, CompilationUnit compilation)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            foreach (var augment in module.Augments)
            {
                try
                {
                    augment.Inject();
                }
                catch (SemanticError error)
                {
                    ReportDiagnostics(context, new ResultOrException<IStatement>(error));
                }
            }
        }
    }

    public void WriteFile(SourceProductionContext context, string fileName, string content)
    {
        context.AddSource(fileName, content);
    }

    private string Clean(string input)
    {
        return string.Join("\n", input.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static void UnwrapUses(SourceProductionContext context, IStatement compilation)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            foreach (var use in module.Uses.Where(use => !use.IsUnderGrouping()))
            {
                try
                {
                    use.Expand();
                }
                catch (SemanticError error)
                {
                    ReportDiagnostics(context, new ResultOrException<IStatement>(error));
                }
            }

            foreach (var identity in module.Identities)
            {
                identity.Expand();
            }
        }
    }

    private static void IncludeSubmodules(SourceProductionContext context, Dictionary<string, Module> modules,
        Dictionary<string, ITopLevelStatement> topLevels)
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
                foreach (var pair in submodule.Usings)
                {
                    module.Usings[pair.Key] = pair.Value;
                }

                module.Rpcs.AddRange(submodule.Rpcs);
                module.Notifications.AddRange(submodule.Notifications);
                module.Identities.AddRange(submodule.Identities);
                module.Actions.AddRange(submodule.Actions);
                module.Imports.AddRange(submodule.Imports);
                module.Groupings.AddRange(submodule.Groupings);
                module.Revisions.AddRange(submodule.Revisions);
                module.Uses.AddRange(submodule.Uses);
                module.HiddenDefinitions.AddRange(submodule.HiddenDefinitions);
                foreach (var pair in submodule.ImportedModules)
                {
                    module.ImportedModules[pair.Key] = pair.Value;
                }
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

    private static readonly DiagnosticDescriptor LogDescriptor = new DiagnosticDescriptor("YANG9999", "DEBUG",
        "LOG: {0}", "DEBUG", DiagnosticSeverity.Warning, true);


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
}