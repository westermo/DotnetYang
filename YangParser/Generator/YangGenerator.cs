using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
        context.RegisterSourceOutput(model, MakeClasses);
    }

    private void MakeClasses(SourceProductionContext context, IStatement statement)
    {
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
                                    namespace {{ns}};
                                    public static class RemoteProcedureCalls
                                    {
                                        public const string Namespace = "{{module.XmlNamespace.Argument}}";
                                        {{string.Join("\n", functions)}}
                                    }
                                    """);
        }
    }

    private void WalkTree(SourceProductionContext context, string Namespace, IStatement statement,
        List<string> functions)
    {
        foreach (var child in statement.Children)
        {
            if (child is IClassSource classSource)
            {
                MakeClass(context, Namespace, classSource, functions);
            }

            if (child is IFunctionSource functionSource)
            {
                functions.Add(MakeFunction(functionSource));
            }
        }
    }

    private string MakeFunction(IFunctionSource functionSource)
    {
        return "dummy";
    }

    private void MakeClass(SourceProductionContext context, string Namespace, IClassSource classSource,
        List<string> functions)
    {
        var name = MakeClassName(classSource.Argument);
        context.AddSource(name, $$"""
                                  using System;

                                  namespace {{Namespace}};
                                  public class {{name}}
                                  {
                                    public string Dummy;
                                  }
                                  """);
        WalkTree(context, Namespace, classSource, functions);
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