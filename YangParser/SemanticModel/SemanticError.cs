using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class SemanticError(string message, YangStatement source) : Exception(message)
{
    public Location Location => Location.Create(
        source.Metadata.Source,
        new TextSpan(
            source.Metadata.Position.Offset,
            source.Metadata.Length
        ),
        new LinePositionSpan(
            new LinePosition(source.Metadata.Position.Line, source.Metadata.Position.Column),
            new LinePosition(source.Metadata.Position.Line, source.Metadata.Position.Column + source.Metadata.Length)
        )
    );
}