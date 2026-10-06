using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using YangParser.Generator;
using YangParser.SemanticModel;

namespace Compiler.Tests;

public class ReferenceResolutionTests
{
    private static string IdentityModule(string name, string derived) =>
        $$"""
          module {{name}} {
              yang-version 1.1;
              namespace "urn:{{name}}";
              prefix {{name.Replace("-", "")}};
              identity base-x;
              identity base-y;
              identity {{derived}} {
                  base base-x;
                  base base-y;
              }
              container top {
                  leaf kind {
                      type identityref {
                          base base-x;
                          base base-y;
                      }
                  }
              }
          }
          """;

    [Test]
    public async Task UnprefixedReferences_ResolveWithinTheirOwnModule()
    {
        // Both modules define identities with identical unprefixed names. Each identityref must resolve them
        // in its own module rather than reusing whichever module happened to be resolved first.
        var sources = Generate(
            ("res-alpha.yang", IdentityModule("res-alpha", "alpha-thing")),
            ("res-beta.yang", IdentityModule("res-beta", "beta-thing")));

        var alpha = sources.Single(s => s.HintName.Contains("res-alpha")).Text;
        var beta = sources.Single(s => s.HintName.Contains("res-beta")).Text;

        await Assert.That(alpha).Contains("AlphaThing");
        await Assert.That(alpha).DoesNotContain("BetaThing");
        await Assert.That(beta).Contains("BetaThing");
        await Assert.That(beta).DoesNotContain("AlphaThing");
    }

    [Test]
    public async Task StableHash_IsProcessIndependent()
    {
        // Values of 32-bit FNV-1a over UTF-16 code units; these must never change, since they end up in
        // generated type names (string.GetHashCode() is randomized per process).
        await Assert.That(StableHash.Compute(string.Empty)).IsEqualTo(-2128831035);
        await Assert.That(StableHash.Compute("/a:b/c")).IsEqualTo(164622047);
        await Assert.That(StableHash.Compute(
                "/dots-signal:dots-signal/dots-signal:message-type/dots-signal:mitigation-scope/dots-signal:scope"))
            .IsEqualTo(-1836959366);
    }

    private static (string HintName, string Text)[] Generate(params (string Path, string Text)[] files)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create("Resolution.Test",
            [CSharpSyntaxTree.ParseText("class A { }", parseOptions)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new YangGenerator().AsSourceGenerator()],
            files.Select(f => (AdditionalText)new InMemoryAdditionalText(f.Path, f.Text)),
            parseOptions);
        var result = driver.RunGenerators(compilation).GetRunResult().Results.Single();
        return result.GeneratedSources.Select(s => (s.HintName, s.SourceText.ToString())).ToArray();
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText m_text = SourceText.From(text);
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => m_text;
    }
}
