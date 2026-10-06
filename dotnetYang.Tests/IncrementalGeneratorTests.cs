using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using YangParser.Generator;

namespace Compiler.Tests;

public class IncrementalGeneratorTests
{
    private const string Yang =
        """
        module caching-test {
            yang-version 1.1;
            namespace "urn:caching:test";
            prefix ct;
            container settings {
                leaf name {
                    type string;
                }
            }
        }
        """;

    private static readonly string[] TrackedSteps =
    [
        YangGenerator.TrackingNames.YangFiles,
        YangGenerator.TrackingNames.AllYangFiles,
        YangGenerator.TrackingNames.AssemblyName,
        YangGenerator.TrackingNames.Features,
        YangGenerator.TrackingNames.GeneratorInput,
    ];

    [Test]
    public async Task UnrelatedCSharpEdit_DoesNotRegenerate()
    {
        var compilation = CreateCompilation("class A { }");
        GeneratorDriver driver = CreateDriver(new InMemoryAdditionalText("caching-test.yang", Yang));

        driver = driver.RunGenerators(compilation);
        var firstRun = driver.GetRunResult();
        await Assert.That(firstRun.GeneratedTrees.Length).IsGreaterThan(0);

        var edited = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            CSharpSyntaxTree.ParseText("class A { int x; }", ParseOptions));
        driver = driver.RunGenerators(edited);
        var secondRun = driver.GetRunResult().Results.Single();

        foreach (var stepName in TrackedSteps)
        {
            var outputs = secondRun.TrackedSteps[stepName].SelectMany(step => step.Outputs);
            foreach (var (_, reason) in outputs)
            {
                await Assert.That(reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
                    .IsTrue().Because($"step '{stepName}' was {reason}");
            }
        }

        foreach (var (_, reason) in secondRun.TrackedOutputSteps.SelectMany(step => step.Value)
                     .SelectMany(step => step.Outputs))
        {
            await Assert.That(reason).IsEqualTo(IncrementalStepRunReason.Cached);
        }

        await Assert.That(GetSources(secondRun)).IsEquivalentTo(GetSources(firstRun.Results.Single()));
    }

    [Test]
    public async Task YangEdit_Regenerates()
    {
        var compilation = CreateCompilation("class A { }");
        var original = new InMemoryAdditionalText("caching-test.yang", Yang);
        GeneratorDriver driver = CreateDriver(original);

        driver = driver.RunGenerators(compilation);
        var firstSources = GetSources(driver.GetRunResult().Results.Single());

        driver = driver.ReplaceAdditionalText(original,
            new InMemoryAdditionalText("caching-test.yang", Yang.Replace("leaf name", "leaf title")));
        driver = driver.RunGenerators(compilation);
        var secondRun = driver.GetRunResult().Results.Single();

        var inputReasons = secondRun.TrackedSteps[YangGenerator.TrackingNames.GeneratorInput]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();
        await Assert.That(inputReasons).Contains(IncrementalStepRunReason.Modified);

        var secondSources = GetSources(secondRun);
        await Assert.That(secondSources.Any(source => source.Contains("Title"))).IsTrue();
        await Assert.That(secondSources).IsNotEquivalentTo(firstSources);
    }

    [Test]
    public async Task RepeatedRuns_ProduceIdenticalOutput()
    {
        // Linking mutates statement trees; re-running the output step must not reuse mutated trees.
        var text = new InMemoryAdditionalText("caching-test.yang", Yang);
        GeneratorDriver driver = CreateDriver(text);
        driver = driver.RunGenerators(CreateCompilation("class A { }"));
        var firstSources = GetSources(driver.GetRunResult().Results.Single());

        // A different assembly name forces the output step to re-run with the same YANG input.
        driver = driver.RunGenerators(CreateCompilation("class A { }", "Other.Assembly"));
        var secondSources = GetSources(driver.GetRunResult().Results.Single())
            .Select(source => source.Replace("Other.Assembly", "Caching.Test"))
            .ToArray();

        await Assert.That(secondSources).IsEquivalentTo(firstSources);
    }

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static CSharpCompilation CreateCompilation(string source, string assemblyName = "Caching.Test") =>
        CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText(source, ParseOptions)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static GeneratorDriver CreateDriver(params AdditionalText[] texts) =>
        CSharpGeneratorDriver.Create(
            [new YangGenerator().AsSourceGenerator()],
            texts,
            ParseOptions,
            optionsProvider: null,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

    private static string[] GetSources(GeneratorRunResult result) =>
        result.GeneratedSources.Select(source => source.SourceText.ToString()).ToArray();

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText m_text = SourceText.From(text);
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => m_text;
    }
}
