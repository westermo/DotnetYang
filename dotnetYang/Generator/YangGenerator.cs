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
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var parsed = yangFiles.Select((p, _) => Parse(p));
        var model = parsed.Select((p, _) => MakeSemanticModel(p));
        var features = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
        {
            provider.GlobalOptions.TryGetValue("build_property.YangFeatures", out var featuresValue);
            return featuresValue ?? string.Empty;
        });
        var combined = context.CompilationProvider
            .Combine(model.Collect())
            .Combine(features);
        context.RegisterSourceOutput(combined, (ctx, data) => MakeClasses(ctx, data.Left.Left, data.Left.Right, data.Right));
    }

    private void MakeClasses(SourceProductionContext context,
        Compilation compilation, ImmutableArray<ResultOrException<IStatement>> models, string enabledFeaturesRaw)
    {
        // Parse enabled features from MSBuild property (semicolon-separated, e.g. "feature1;feature2")
        var enabledFeatures = string.IsNullOrWhiteSpace(enabledFeaturesRaw)
            ? null
            : new HashSet<string>(enabledFeaturesRaw.Split(';').Select(f => f.Trim()).Where(f => f.Length > 0));

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
                        if (modules.TryGetValue(module.Argument, out var existing))
                        {
                            // Multiple revisions of the same module: keep the latest revision.
                            var existingRevision = existing.Revisions.OrderByDescending(r => r.Value).FirstOrDefault();
                            var newRevision = module.Revisions.OrderByDescending(r => r.Value).FirstOrDefault();
                            if (newRevision != null && (existingRevision == null || newRevision.Value > existingRevision.Value))
                            {
                                // New module is newer — replace
                                topLevels[module.Argument] = module;
                                modules[module.Argument] = module;
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        MultipleRevisionsWarning,
                                        Location.None,
                                        module.Argument,
                                        newRevision.Argument,
                                        existingRevision?.Argument ?? "(none)"));
                            }
                            else
                            {
                                // Existing is same or newer — keep existing, report diagnostic
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        MultipleRevisionsWarning,
                                        Location.None,
                                        module.Argument,
                                        existingRevision?.Argument ?? "(none)",
                                        newRevision?.Argument ?? "(none)"));
                            }
                        }
                        else
                        {
                            topLevels[module.Argument] = module;
                            modules[module.Argument] = module;
                        }
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
            var compilationUnit = new CompilationUnit(modules.Values.ToArray(), compilation.AssemblyName!);
            //Replace Uses by their respective groupings
            UnwrapUses(context, compilationUnit);
            InjectAugments(context, compilationUnit);
            ApplyDeviations(context, compilationUnit);
            ValidateYangVersion(context, compilationUnit);
            if (enabledFeatures != null)
            {
                PruneUnsupportedFeatures(compilationUnit, enabledFeatures);
            }
            foreach (var module in compilationUnit.Children.OfType<Module>())
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

            WriteFile(context, "Configuration.cs", compilationUnit.ToCode());
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

    private void ApplyDeviations(SourceProductionContext context, CompilationUnit compilation)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            foreach (var deviation in module.Deviations)
            {
                try
                {
                    deviation.Apply();
                }
                catch (SemanticError error)
                {
                    ReportDiagnostics(context, new ResultOrException<IStatement>(error));
                }
            }
        }
    }

    /// <summary>
    /// Validates that YANG 1.0 modules do not use YANG 1.1-only constructs.
    /// Per RFC 6020/RFC 7950: action, anydata, and notification-in-data-nodes are 1.1-only.
    /// Reports warnings (not errors) since the code generator can still produce valid output.
    /// </summary>
    private static void ValidateYangVersion(SourceProductionContext context, CompilationUnit compilation)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            var versionStmt = module.Children.OfType<YangVersion>().FirstOrDefault();
            var version = versionStmt?.Argument?.Trim();
            // YANG 1.1 uses "1.1"; absence or "1" means YANG 1.0
            if (version == "1.1") continue;

            // Check for 1.1-only constructs in this YANG 1.0 module
            foreach (var node in module.Unwrap())
            {
                switch (node)
                {
                    case SemanticModel.Action a:
                        context.ReportDiagnostic(Diagnostic.Create(
                            Yang10CompatibilityWarning, Location.None,
                            module.Argument, "action", a.Argument));
                        break;
                    case AnyData ad:
                        context.ReportDiagnostic(Diagnostic.Create(
                            Yang10CompatibilityWarning, Location.None,
                            module.Argument, "anydata", ad.Argument));
                        break;
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
                module.Deviations.AddRange(submodule.Deviations);
                module.HiddenDefinitions.AddRange(submodule.HiddenDefinitions);
                foreach (var pair in submodule.ImportedModules)
                {
                    module.ImportedModules[pair.Key] = pair.Value;
                }

                foreach (var pair in submodule.PrefixToNamespaceTable)
                {
                    if (!module.PrefixToNamespaceTable.ContainsKey(pair.Key))
                    {
                        module.PrefixToNamespaceTable[pair.Key] = pair.Value;
                    }
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

    private static readonly DiagnosticDescriptor MultipleRevisionsWarning = new DiagnosticDescriptor("YANG0003",
        "Multiple Module Revisions",
        "Module '{0}' has multiple revisions; using revision '{1}' (discarding '{2}')",
        "SemanticModel", DiagnosticSeverity.Warning, true);

    private static readonly DiagnosticDescriptor Yang10CompatibilityWarning = new DiagnosticDescriptor("YANG0004",
        "YANG 1.0 Compatibility",
        "YANG 1.0 module '{0}' uses YANG 1.1-only construct '{1}' at {2}",
        "SemanticModel", DiagnosticSeverity.Warning, true);


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
        var ts = Stopwatch.GetTimestamp();
        try
        {
            return new ResultOrException<YangStatement>(Parser.Parser.Parse(text.Path, text.GetText()!.ToString()));
        }
        catch (Exception ex)
        {
            ex.Data["textFile"] = text;
            return new ResultOrException<YangStatement>(ex);
        }
        finally
        {
            Log.Write("Parsed file " + text.Path + " in " +
                      ((Stopwatch.GetTimestamp() - ts) / (double)Stopwatch.Frequency * 1000).ToString("F2") + "ms");
        }
    }

    /// <summary>
    /// Removes schema nodes that have an if-feature referencing a feature not in the enabled set.
    /// Per RFC 7950 §7.20.2, a node with if-feature is only valid when all referenced features are supported.
    /// </summary>
    private static void PruneUnsupportedFeatures(IStatement root, HashSet<string> enabledFeatures)
    {
        // Collect nodes to prune (breadth-first to avoid modifying while iterating).
        var toPrune = new System.Collections.Generic.List<(IStatement parent, IStatement child)>();
        CollectUnsupportedNodes(root, enabledFeatures, toPrune);
        foreach (var (parent, child) in toPrune)
        {
            parent.Replace(child, Array.Empty<IStatement>());
        }
    }

    private static void CollectUnsupportedNodes(IStatement node, HashSet<string> enabledFeatures,
        System.Collections.Generic.List<(IStatement parent, IStatement child)> toPrune)
    {
        foreach (var child in node.Children.ToArray())
        {
            // Check if this child has if-feature statements
            var ifFeatures = child.Children.OfType<FeatureFlag>().ToArray();
            if (ifFeatures.Length > 0)
            {
                // All if-feature conditions must be satisfied (AND semantics per RFC 7950 §7.20.2)
                var allSupported = ifFeatures.All(ff => IsFeatureExpression(ff.Argument, enabledFeatures));
                if (!allSupported)
                {
                    toPrune.Add((node, child));
                    continue; // Don't recurse into pruned nodes
                }
            }

            // Recurse into children
            CollectUnsupportedNodes(child, enabledFeatures, toPrune);
        }
    }

    /// <summary>
    /// Evaluates a simple if-feature expression. Supports:
    /// - Simple feature name: "my-feature"
    /// - Prefix:feature: "prefix:feature" (prefix stripped for lookup)
    /// - not: "not feature"  
    /// - and/or: "feature1 and feature2", "feature1 or feature2"
    /// </summary>
    private static bool IsFeatureExpression(string expr, HashSet<string> enabledFeatures)
    {
        expr = expr.Trim();

        // Handle "not" prefix (YANG 1.1)
        if (expr.StartsWith("not "))
        {
            return !IsFeatureExpression(expr.Substring(4), enabledFeatures);
        }

        // Handle "and" / "or" (YANG 1.1 §7.20.2)
        var andIdx = expr.IndexOf(" and ");
        if (andIdx > 0)
        {
            return IsFeatureExpression(expr.Substring(0, andIdx), enabledFeatures)
                && IsFeatureExpression(expr.Substring(andIdx + 5), enabledFeatures);
        }

        var orIdx = expr.IndexOf(" or ");
        if (orIdx > 0)
        {
            return IsFeatureExpression(expr.Substring(0, orIdx), enabledFeatures)
                || IsFeatureExpression(expr.Substring(orIdx + 4), enabledFeatures);
        }

        // Strip prefix if present (e.g., "mod:feature" → "feature")
        var colonIdx = expr.IndexOf(':');
        var featureName = colonIdx >= 0 ? expr.Substring(colonIdx + 1) : expr;
        return enabledFeatures.Contains(featureName);
    }
}