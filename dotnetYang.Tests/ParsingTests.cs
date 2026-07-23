using System.Text;
using YangParser;
using YangParser.Generator;
using YangParser.Parser;
using YangParser.SemanticModel;

namespace Compiler.Tests;

public class ParsingTests
{
    [Test]
    public async Task IdentityTest()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module test {
                prefix this;
                yang-version 1.1;
                namespace "identity:container";
                identity a;
                identity b {
                    base a;
                }
                identity c {
                    base b;
                }
                identity d {
                    base c;
                    base a;
                }
                leaf testy {
                    type identityref {
                        base a;
                    }
                }
                typedef test-2 {
                    type identityref {
                        base c;
                        base b;
                    }
                }
                leaf test-3 {
                    type identityref {
                        base c;
                        base b;
                    }
                }
                leaf testa {
                    type test-2;
                }
                
                identity site-role {
                 description
                 "Base identity for site type.";
                }
                identity any-to-any-role {
                 base site-role;
                 description
                 "Site in an any-to-any IP VPN.";
                }
                identity spoke-role {
                 base site-role;
                 description
                 "Spoke site in a Hub-and-Spoke IP VPN.";
                }
                identity hub-role {
                 base site-role;
                 description
                 "Hub site in a Hub-and-Spoke IP VPN.";
                }
                identity vpn-topology {
                 description
                 "Base identity for VPN topology.";
                }
                identity any-to-any {
                 base vpn-topology;
                 description
                 "Identity for any-to-any VPN topology.";
                }
                identity hub-spoke {
                 base vpn-topology;
                 description
                 "Identity for Hub-and-Spoke VPN topology.";
                }
                identity hub-spoke-disjoint {
                 base vpn-topology;
                 description
                 "Identity for Hub-and-Spoke VPN topology
                 where Hubs cannot communicate with each other.";
                }
            }
            """));
        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            foreach (var identity in module.Identities)
            {
                identity.Expand();
            }

            var code = top.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("public enum AIdentity");
            await Assert.That(code).Contains("public enum BIdentity");
            await Assert.That(code).Contains("public enum CIdentity");
            await Assert.That(code).Contains("public enum DIdentity");
        }
    }

    [Test]
    public async Task AugmentationIsFoundTest()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module test {
                prefix this;
                yang-version 1.1;
                namespace "urn:ns:test";
                augment a;
                augment b;
            }
            """));
        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            await Assert.That(module.Augments.Count).IsEqualTo(2);
            await Assert.That(module.Augments[0].Argument).IsEqualTo("a");
            await Assert.That(module.Augments[1].Argument).IsEqualTo("b");
        }
    }

    [Test]
    public void BaseParsingTest()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        Print(statements);
    }

    public const string ModuleOne = """
                                    module one {
                                        yang-version 1.1;
                                        namespace "urn:ns:one";
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
                                        namespace "urn:ns:two";
                                        prefix two;
                                        import three {
                                            prefix b;
                                        }
                                        grouping A {
                                            uses b:A {
                                                refine target {
                                                    default 80;
                                                }
                                            }
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
                                          namespace "urn:NS:three";
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
                                              leaf target { type int32; }
                                          }
                                      }
                                      """;

    [Test]
    public async Task GroupingTest()
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
        IncludeSubmodules(compilationUnit.Children.OfType<Module>().ToDictionary(x => x.Argument),
            compilationUnit.Children.ToDictionary(x => x.Argument));
        foreach (var module in compilationUnit.Children.OfType<Module>())
        {
            foreach (var use in module.Uses)
            {
                if (use.IsUnderGrouping())
                {
                    continue;
                }

                use.Expand();
            }
        }

        foreach (var statement in compilationUnit.Unwrap())
        {
            if (statement.IsUnderGrouping()) continue;
            if (statement is Uses uses)
            {
                Console.WriteLine(uses.Parent!.ToString());
            }

            await Assert.That(statement).IsNotTypeOf<Uses>();
        }

        Console.WriteLine(compilationUnit.ToCode());
        foreach (var child in compilationUnit.Children)
        {
            Console.WriteLine(child.ToCode());
        }

        Log.Clear();
    }

    [Test]
    public async Task ReplacementTest()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        var preLength = statements.Children.Length;
        var original = statements.Children[2];
        statements.Replace(statements.Children[2], statements.Children);
        await Assert.That(statements.Children.Length).IsEqualTo(preLength);
        await Assert.That(statements.Children[^1]).IsEqualTo(original);
    }

    [Test]
    public async Task UnwrapTest()
    {
        var result = Parser.Parse("memory", File.ReadAllText("ietf-inet-types@2013-07-15.yang"));
        var statements = StatementFactory.Create(result);
        var array = statements.Unwrap().ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            for (int j = i + 1; j < array.Length; j++)
            {
                await Assert.That(ReferenceEquals(array[i], array[j])).IsFalse();
            }
        }
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

        Console.WriteLine($"{tabs}{statement.GetType().Name} {statement.Argument}{terminator}");
        if (statement.Children.Length <= 0) return;
        Console.WriteLine($"{tabs}{{");
        foreach (var sub in statement.Children) Print(sub, indent + 1);
        Console.WriteLine($"{tabs}}}");
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

    #region Deviation Tests

    [Test]
    public async Task DeviateNotSupportedRemovesNode()
    {
        string[] sources =
        [
            """
            module target-mod {
                yang-version 1.1;
                namespace "urn:ns:target";
                prefix tm;
                container config {
                    leaf hostname {
                        type string;
                        mandatory true;
                    }
                    leaf deprecated-setting {
                        type string;
                    }
                }
            }
            """,
            """
            module deviating-mod {
                yang-version 1.1;
                namespace "urn:ns:deviating";
                prefix dm;
                import target-mod {
                    prefix tm;
                }
                deviation /tm:config/tm:deprecated-setting {
                    deviate not-supported;
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);
        ApplyDeviations(compilation, modules);

        var targetModule = modules["target-mod"];
        var code = targetModule.ToCode();
        Console.WriteLine(code);

        await Assert.That(code).Contains("Hostname");
        await Assert.That(code).DoesNotContain("DeprecatedSetting");
    }

    [Test]
    public async Task DeviateAddAppendsProperties()
    {
        string[] sources =
        [
            """
            module target-mod {
                yang-version 1.1;
                namespace "urn:ns:target";
                prefix tm;
                container config {
                    leaf port {
                        type int32;
                    }
                }
            }
            """,
            """
            module deviating-mod {
                yang-version 1.1;
                namespace "urn:ns:deviating";
                prefix dm;
                import target-mod {
                    prefix tm;
                }
                deviation /tm:config/tm:port {
                    deviate add {
                        default 8080;
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);
        ApplyDeviations(compilation, modules);

        var targetModule = modules["target-mod"];
        var configContainer = targetModule.Unwrap().First(c => c.Argument == "config");
        var portLeaf = configContainer.Children.First(c => c.Argument == "port");
        var hasDefault = portLeaf.Children.Any(c => c is DefaultValue);
        await Assert.That(hasDefault).IsTrue();
    }

    [Test]
    public async Task DeviateReplaceReplacesProperty()
    {
        string[] sources =
        [
            """
            module target-mod {
                yang-version 1.1;
                namespace "urn:ns:target";
                prefix tm;
                container config {
                    leaf port {
                        type int32;
                        default 80;
                    }
                }
            }
            """,
            """
            module deviating-mod {
                yang-version 1.1;
                namespace "urn:ns:deviating";
                prefix dm;
                import target-mod {
                    prefix tm;
                }
                deviation /tm:config/tm:port {
                    deviate replace {
                        default 443;
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);
        ApplyDeviations(compilation, modules);

        var targetModule = modules["target-mod"];
        var portLeaf = targetModule.Unwrap().First(c => c.Argument == "port");
        var defaultValue = portLeaf.Children.OfType<DefaultValue>().FirstOrDefault();
        await Assert.That(defaultValue).IsNotNull();
        await Assert.That(defaultValue!.Argument).IsEqualTo("443");
    }

    [Test]
    public async Task DeviateDeleteRemovesMatchingProperty()
    {
        string[] sources =
        [
            """
            module target-mod {
                yang-version 1.1;
                namespace "urn:ns:target";
                prefix tm;
                container config {
                    leaf port {
                        type int32;
                        must "../hostname";
                    }
                    leaf hostname {
                        type string;
                    }
                }
            }
            """,
            """
            module deviating-mod {
                yang-version 1.1;
                namespace "urn:ns:deviating";
                prefix dm;
                import target-mod {
                    prefix tm;
                }
                deviation /tm:config/tm:port {
                    deviate delete {
                        must "../hostname";
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);
        ApplyDeviations(compilation, modules);

        var targetModule = modules["target-mod"];
        var portLeaf = targetModule.Unwrap().First(c => c.Argument == "port");
        var hasMust = portLeaf.Children.Any(c => c is Must);
        await Assert.That(hasMust).IsFalse();
    }

    #endregion

    #region Refine Tests

    [Test]
    public async Task RefineSingletonReplacesExistingDefault()
    {
        string[] sources =
        [
            """
            module refine-mod {
                yang-version 1.1;
                namespace "urn:ns:refine";
                prefix rm;
                grouping server-config {
                    leaf port {
                        type int32;
                        default 80;
                    }
                }
                container servers {
                    uses server-config {
                        refine port {
                            default 443;
                        }
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);

        var mod = modules["refine-mod"];
        Log.Clear();
        var serversContainer = mod.Unwrap().First(c => c is Container && c.Argument == "servers");
        var portLeaf = serversContainer.Children.First(c => c is Leaf && c.Argument == "port");
        var defaults = portLeaf.Children.OfType<DefaultValue>().ToArray();
        await Assert.That(defaults.Length).IsEqualTo(1);
        await Assert.That(defaults[0].Argument).IsEqualTo("443");
    }

    [Test]
    public async Task RefineMandatoryReplacesExisting()
    {
        string[] sources =
        [
            """
            module refine-mod {
                yang-version 1.1;
                namespace "urn:ns:refine";
                prefix rm;
                grouping server-config {
                    leaf hostname {
                        type string;
                    }
                }
                container servers {
                    uses server-config {
                        refine hostname {
                            mandatory true;
                        }
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);

        var mod = modules["refine-mod"];
        Log.Clear();
        var serversContainer = mod.Unwrap().First(c => c is Container && c.Argument == "servers");
        var hostnameLeaf = serversContainer.Children.First(c => c is Leaf && c.Argument == "hostname");
        var mandatory = hostnameLeaf.Children.OfType<Mandatory>().FirstOrDefault();
        await Assert.That(mandatory).IsNotNull();
        await Assert.That(mandatory!.Argument).IsEqualTo("true");
    }

    #endregion

    #region AnyXml/AnyData Tests

    [Test]
    public async Task AnyXmlGeneratesParseSupport()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module anyxml-mod {
                yang-version 1.1;
                namespace "urn:ns:anyxml";
                prefix am;
                container config {
                    anyxml filter;
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("Filter");
            await Assert.That(code).Contains("string?");
            await Assert.That(code).Contains("ReadInnerXml");
        }
    }

    [Test]
    public async Task AnyDataGeneratesParseSupport()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module anydata-mod {
                yang-version 1.1;
                namespace "urn:ns:anydata";
                prefix ad;
                container state {
                    anydata content;
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("Content");
            await Assert.That(code).Contains("string?");
            await Assert.That(code).Contains("ReadInnerXml");
        }
    }

    #endregion

    #region Grouping with action/notification Tests

    [Test]
    public async Task GroupingWithActionIsParsed()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module action-grouping-mod {
                yang-version 1.1;
                namespace "urn:ns:actgrp";
                prefix ag;
                grouping my-group {
                    action reset {
                        input {
                            leaf reason {
                                type string;
                            }
                        }
                    }
                }
                container server {
                    uses my-group;
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var grouping = module.Groupings.First();
            await Assert.That(grouping.Children.Any(c => c is YangParser.SemanticModel.Action)).IsTrue();
        }
    }

    [Test]
    public async Task GroupingWithNotificationIsParsed()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module notif-grouping-mod {
                yang-version 1.1;
                namespace "urn:ns:notifgrp";
                prefix ng;
                grouping my-group {
                    notification link-down {
                        leaf interface {
                            type string;
                        }
                    }
                }
                container interfaces {
                    uses my-group;
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var grouping = module.Groupings.First();
            await Assert.That(grouping.Children.Any(c => c is Notification)).IsTrue();
        }
    }

    #endregion

    #region Must error-app-tag Test

    [Test]
    public async Task MustIncludesErrorAppTagInAttribute()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module must-mod {
                yang-version 1.1;
                namespace "urn:ns:must";
                prefix mm;
                container config {
                    leaf port {
                        type int32;
                        must ". > 0 and . < 65536" {
                            error-app-tag "invalid-port";
                            error-message "Port must be between 1 and 65535";
                        }
                    }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("errorAppTag");
            await Assert.That(code).Contains("invalid-port");
            await Assert.That(code).Contains("Port must be between 1 and 65535");
        }
    }

    [Test]
    public async Task MinMaxElements_GeneratesValidationCode()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module minmax-mod {
                yang-version 1.1;
                namespace "urn:ns:minmax";
                prefix mm;
                container config {
                    list servers {
                        key "name";
                        min-elements 1;
                        max-elements 10;
                        leaf name { type string; }
                    }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("min-elements");
            await Assert.That(code).Contains("max-elements");
            await Assert.That(code).Contains("YangValidationException");
        }
    }

    [Test]
    public async Task UniqueConstraint_GeneratesValidationCode()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module unique-mod {
                yang-version 1.1;
                namespace "urn:ns:unique";
                prefix um;
                container data {
                    list users {
                        key "id";
                        unique "email";
                        leaf id { type uint32; }
                        leaf email { type string; }
                    }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("unique");
            await Assert.That(code).Contains("HashSet");
            await Assert.That(code).Contains("YangValidationException");
        }
    }

    [Test]
    public async Task IfFeature_PrunesNodesWhenFeatureNotEnabled()
    {
        string[] sources =
        [
            """
            module feat-mod {
                yang-version 1.1;
                namespace "urn:ns:feat";
                prefix fm;
                feature advanced;
                container config {
                    leaf basic-setting {
                        type string;
                    }
                    leaf advanced-setting {
                        if-feature "advanced";
                        type string;
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);

        // Prune without "advanced" feature enabled
        var enabledFeatures = new HashSet<string> { "basic" }; // "advanced" not included
        PruneUnsupportedFeatures(compilation, enabledFeatures);

        var code = modules["feat-mod"].ToCode();
        Console.WriteLine(code);

        await Assert.That(code).Contains("BasicSetting");
        await Assert.That(code).DoesNotContain("AdvancedSetting");
    }

    [Test]
    public async Task IfFeature_KeepsNodesWhenFeatureEnabled()
    {
        string[] sources =
        [
            """
            module feat-mod {
                yang-version 1.1;
                namespace "urn:ns:feat";
                prefix fm;
                feature advanced;
                container config {
                    leaf basic-setting {
                        type string;
                    }
                    leaf advanced-setting {
                        if-feature "advanced";
                        type string;
                    }
                }
            }
            """
        ];

        var (compilation, modules) = BuildCompilation(sources);

        // Enable "advanced" feature
        var enabledFeatures = new HashSet<string> { "advanced" };
        PruneUnsupportedFeatures(compilation, enabledFeatures);

        var code = modules["feat-mod"].ToCode();
        Console.WriteLine(code);

        await Assert.That(code).Contains("BasicSetting");
        await Assert.That(code).Contains("AdvancedSetting");
    }

    [Test]
    public async Task MaxElements_UnboundedDoesNotGenerateValidation()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module unbound-mod {
                yang-version 1.1;
                namespace "urn:ns:unbound";
                prefix ub;
                container data {
                    list items {
                        key "id";
                        max-elements unbounded;
                        leaf id { type string; }
                    }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            // "unbounded" should not generate a max-elements check
            await Assert.That(code).DoesNotContain("max-elements");
        }
    }

    [Test]
    public async Task GetChild_MethodIsGenerated()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module getchild-mod {
                yang-version 1.1;
                namespace "urn:ns:getchild";
                prefix gc;
                container settings {
                    leaf name { type string; }
                    leaf value { type uint32; }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            await Assert.That(code).Contains("GetChild");
            await Assert.That(code).Contains("\"name\"");
            await Assert.That(code).Contains("\"value\"");
        }
    }

    [Test]
    public async Task OrderedByUser_GeneratesInsertAttribute()
    {
        var top = StatementFactory.Create(Parser.Parse("memory",
            """
            module ordered-mod {
                yang-version 1.1;
                namespace "urn:ns:ordered";
                prefix om;
                container data {
                    list entries {
                        key "name";
                        ordered-by user;
                        leaf name { type string; }
                    }
                }
            }
            """));

        await Assert.That(top).IsTypeOf<Module>();
        if (top is Module module)
        {
            var code = module.ToCode();
            Console.WriteLine(code);
            // Should contain yang:insert parsing logic
            await Assert.That(code).Contains("insert");
            await Assert.That(code).Contains("urn:ietf:params:xml:ns:yang:1");
        }
    }

    #endregion

    #region Helper Methods

    private (CompilationUnit compilation, Dictionary<string, Module> modules) BuildCompilation(string[] sources)
    {
        List<IStatement> statements = new();
        foreach (var src in sources)
        {
            var result = Parser.Parse("memory", src);
            statements.Add(StatementFactory.Create(result));
        }

        var modules = statements.OfType<Module>().ToDictionary(m => m.Argument);
        var topLevels = statements.ToDictionary(s => s.Argument);
        IncludeSubmodules(modules, topLevels);

        var compilation = new CompilationUnit(modules.Values.ToArray());

        foreach (var module in compilation.Children.OfType<Module>())
        {
            foreach (var use in module.Uses.Where(u => !u.IsUnderGrouping()))
            {
                use.Expand();
            }

            foreach (var identity in module.Identities)
            {
                identity.Expand();
            }
        }

        return (compilation, modules);
    }

    private static void ApplyDeviations(CompilationUnit compilation, Dictionary<string, Module> modules)
    {
        foreach (var module in compilation.Children.OfType<Module>())
        {
            foreach (var deviation in module.Deviations)
            {
                deviation.Apply();
            }
        }
    }

    private static void PruneUnsupportedFeatures(IStatement root, HashSet<string> enabledFeatures)
    {
        var toPrune = new List<(IStatement parent, IStatement child)>();
        CollectUnsupported(root, enabledFeatures, toPrune);
        foreach (var (parent, child) in toPrune)
        {
            parent.Replace(child, Array.Empty<IStatement>());
        }
    }

    private static void CollectUnsupported(IStatement node, HashSet<string> enabledFeatures,
        List<(IStatement, IStatement)> toPrune)
    {
        foreach (var child in node.Children.ToArray())
        {
            var ifFeatures = child.Children.OfType<FeatureFlag>().ToArray();
            if (ifFeatures.Length > 0)
            {
                var allSupported = ifFeatures.All(ff => enabledFeatures.Contains(ff.Argument.Trim()));
                if (!allSupported)
                {
                    toPrune.Add((node, child));
                    continue;
                }
            }
            CollectUnsupported(child, enabledFeatures, toPrune);
        }
    }

    #endregion
}