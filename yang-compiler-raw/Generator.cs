using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Yang.Compiler;

[Generator]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var yangFiles = context.AdditionalTextsProvider.Where(text => text.Path.EndsWith(".yang"));
        var models = yangFiles.Select((t, ct) => t.GetText(ct)).Where(t => t is not null).Select(MakeSemanticModel);
        context.RegisterSourceOutput(models, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, YangSemanticModel semanticModel)
    {
        throw new NotImplementedException();
    }

    private YangSemanticModel MakeSemanticModel(SourceText? text, CancellationToken token)
    {
        return YangSemanticModel.Parse(text!);
    }
}