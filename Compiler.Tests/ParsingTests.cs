using System.Text;
using Xunit.Abstractions;
using YangParser;
using YangParser.Parser;
using YangParser.SemanticModel;

namespace Compiler.Tests;

public class ParsingTests(ITestOutputHelper output)
{
    [Fact]
    public void IetfYangLibrary()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        Print(statements);
    }

    public const string ModuleOne = """
                                    module one {
                                        yang-version 1.1;
                                        namespace "urn:one";
                                        prefix one;
                                        import two {
                                            prefix b;
                                        }
                                        container start {
                                            uses A;
                                        }
                                        container other {
                                            uses B;
                                            grouping inner {
                                                uses A;
                                            }
                                            uses inner;
                                        }
                                        grouping B {
                                            uses A;
                                        }
                                        grouping A {
                                            uses b:A;
                                        }
                                    }
                                    """;

    public const string ModuleTwo = """
                                    module two {
                                        yang-version 1.1;
                                        namespace "urn:two";
                                        prefix two;
                                        import three {
                                            prefix b;
                                        }
                                        grouping A {
                                            uses b:A;
                                            container C {
                                                uses C;
                                            }
                                        }
                                        
                                        grouping C {
                                            container modulesu-state {
                                            config false;
                                            description "Contains YANG module monitoring information.";
                                            leaf module-set-idu {
                                                type string;
                                                mandatory true;
                                                description
                                                "Contains a server-specific identifier representing the current set of modules and submodules.  The
                                                server MUST change the value of this leaf if the information represented by the 'module' list instances
                                                has changed.";
                                                }
                                            }
                                        }
                                    }
                                    """;

    public const string ModuleThree = """
                                      module three {
                                          yang-version 1.1;
                                          namespace "urn:three";
                                          prefix three;
                                          typedef operator {
                                            type bits {
                                                bit not {
                                                    position 0;
                                                    description "If set, logical negation of operation.";
                                                }
                                                bit match {
                                                    position 1;
                                                    description "Match bit.  This is a bitwise match operation defined as '(data & value) == value'.";
                                                }
                                                bit any {
                                                    position 3;
                                                    description "Any bit.  This is a match on any of the bits in bitmask.  It evaluates to 'true' if any of the bits in the value mask are set in the data, i.e., '(data & value) != 0'.";
                                                }
                                            }
                                            description "Specifies how to apply the defined bitmask. 'any' and 'match' bits must not be set simultaneously.";
                                        }
                                          grouping A {
                                              container modules-state {
                                                  config false;
                                                  description "Contains YANG module monitoring information.";
                                                  leaf module-set-id {
                                                      type string;
                                                      mandatory true;
                                                      description
                                                      "Contains a server-specific identifier representing the current set of modules and submodules.  The
                                                      server MUST change the value of this leaf if the information represented by the 'module' list instances
                                                      has changed.";
                                                  }
                                                  leaf test { type operator; }
                                              }
                                          }
                                      }
                                      """;

    [Fact]
    public void GroupingTest()
    {
        string[] sources = [ModuleOne, ModuleTwo, ModuleThree];
        List<IStatement> modules = new();
        foreach (var src in sources)
        {
            var result = Parser.Parse("memory", src);
            var statements = StatementFactory.Create(result);
            modules.Add(statements);
        }

        CompilationUnit compilationUnit = new CompilationUnit(modules.OfType<Module>().ToArray());
        foreach (var module in compilationUnit.Children.OfType<Module>())
        {
            var usings = module.Unwrap().OfType<Uses>().ToArray();
            foreach (var use in usings)
            {
                if (use.IsUnderGrouping())
                {
                    continue;
                }

                if (use.Parent == null) continue;

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
        }

        foreach (var statement in compilationUnit.Unwrap())
        {
            Assert.IsNotType<Uses>(statement);
        }

        IncludeSubmodules(compilationUnit.Children.OfType<Module>().ToDictionary(x => x.Argument),
            compilationUnit.Children.ToDictionary(x => x.Argument));
        UnwrapUses(compilationUnit);

        output.WriteLine(Clean(compilationUnit.ToCode()));
    }

    [Fact]
    public void ReplacementTest()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        var preLength = statements.Children.Length;
        var original = statements.Children[2];
        statements.Replace(statements.Children[2], statements.Children);
        Assert.Equal(preLength, statements.Children.Length);
        Assert.Equal(original, statements.Children[^1]);
    }

    [Fact]
    public void UnwrapTest()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        var array = statements.Unwrap().ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            for (int j = i + 1; j < array.Length; j++)
            {
                Assert.False(ReferenceEquals(array[i], array[j]));
            }
        }
    }

    private void Print(YangStatement statement, int indent = 0)
    {
        var terminator = statement.Children.Count == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.WriteLine($"{tabs}{statement.Prefix}{statement.Keyword} {statement.Argument}{terminator}");
        if (statement.Children.Count <= 0) return;
        output.WriteLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
        output.WriteLine($"{tabs}}}");
    }

    private string Clean(string input)
    {
        return string.Join("\n", input.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private void Print(IStatement statement, int indent = 0)
    {
        var terminator = statement.Children.Length == 0 ? ";" : "";
        var tabs = new StringBuilder();
        for (var i = 0; i < indent; i++)
        {
            tabs.Append('\t');
        }

        output.WriteLine($"{tabs}{statement.GetType().Name} {statement.Argument}{terminator}");
        if (statement.Children.Length <= 0) return;
        output.WriteLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
        output.WriteLine($"{tabs}}}");
    }

    private static void UnwrapUses(IStatement compilation)
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

                use.Expand();
            }
        }
    }

    private static void IncludeSubmodules(Dictionary<string, Module> modules,
        Dictionary<string, IStatement> topLevels)
    {
        foreach (var module in modules.Values)
        {
            var includes = module.Unwrap().OfType<Include>().ToArray();
            foreach (var include in includes)
            {
                if (!topLevels.TryGetValue(include.Argument, out var submodule))
                {
                    throw new SemanticError(
                        $"Could not find a subModule with the key {include.Argument}",
                        include.Source);
                }

                if (submodule.TryGetChild<BelongsTo>(out var belongsTo))
                {
                    if (module.Argument != belongsTo?.Argument)
                    {
                        throw new SemanticError(
                            $"Include of module {submodule.Argument} that does not belong to module {module.Argument} (belongs to {belongsTo?.Argument})",
                            include.Source);
                    }
                }

                include.Parent?.Replace(include, submodule.Children);
            }
        }
    }
}