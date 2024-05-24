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
        context.RegisterPostInitializationOutput(AddAttributesClass);
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var parsed = yangFiles.Select((p, _) => Parse(p));
        var model = parsed.Select((p, _) => MakeSemanticModel(p));
        context.RegisterSourceOutput(model.Collect(), MakeClasses);
    }

    private void MakeClasses(SourceProductionContext context, ImmutableArray<ResultOrException<IStatement>> models)
    {
        Log.Clear();
        try
        {
            Dictionary<string, ITopLevelStatement> topLevels = new();
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
                    default:
                        throw new SemanticError(
                            $"Unexpected top level statement of type {statement.GetType().Name} from {statement.Argument}",
                            statement.Source);
                }
            }

            //Replace Includes with their respective submodules
            IncludeSubmodules(context, modules, topLevels);
            var compilation = new CompilationUnit(modules.Values.ToArray());
            Log.Write(
                $"available modules are:\n-{string.Join("\n-", compilation.Children.OfType<Module>().Select(i => i.Argument))}");


            //Replace Uses by their respective groupings
            UnwrapUses(context, compilation);
            foreach (var module in compilation.Children.OfType<Module>())
            {
                try
                {
                    context.AddSource(module.Filename, Clean(module.ToCode()));
                }
                catch (Exception e)
                {
                    context.AddSource(module.Filename + ".errors",
                        $"#error Exception when generating code for {module.Filename}" + "\n/*\n" + e.Message + "\n" +
                        e.StackTrace + "\n*/");
                }
            }
        }
        catch (Exception e)
        {
            context.AddSource("errors",
                "#error General Exception" + "\n/*\n" + e.Message + "\n" + e.StackTrace + "\n*/");
        }

        context.AddSource("log.cs", "/*\n" + Log.Get() + "\n*/");
    }

    private string Clean(string input)
    {
        return string.Join("\n", input.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
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
                    use.Expand();
                }
                catch (SemanticError error)
                {
                    ReportDiagnostics(context, new ResultOrException<IStatement>(error));
                }
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
                Log.Write($"Including submodule '{submodule.Argument}' into module '{module.Argument}'");
                foreach (var pair in submodule.Usings)
                {
                    module.Usings[pair.Key] = pair.Value;
                    Log.Write($"Added prefix '{pair.Key}' as '{pair.Value}' to '{module.Argument}'");
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
        context.AddSource("YangModules/Attributes/Yang.Attributes.cs", """
                                                                       using System;
                                                                       namespace Yang.Attributes;
                                                                       [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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
                                                                       public class KeyAttribute(params string[] value) : Attribute
                                                                       {
                                                                           public string[] Value { get; } = value;
                                                                       }
                                                                       [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                                       public class MinElementsAttribute(int value) : Attribute
                                                                       {
                                                                           public int Value { get; } = value;
                                                                       }
                                                                       [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                                       public class MaxElementsAttribute(int value) : Attribute
                                                                       {
                                                                           public int Value { get; } = value;
                                                                       }
                                                                       [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                                       public class OrderedByAttribute(string value) : Attribute
                                                                       {
                                                                           public string Value { get; } = value;
                                                                       }
                                                                       [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
                                                                       public class NotConfigurationData : Attribute;

                                                                       public class InstanceIdentifier(string path)
                                                                       {
                                                                            public string Path { get; } = path;
                                                                       }
                                                                       public interface IChannel
                                                                       {
                                                                           string Send(string xml);
                                                                       }
                                                                       public interface IXMLSource
                                                                       {
                                                                           string ToXML();
                                                                       }
                                                                       """);
    }
}