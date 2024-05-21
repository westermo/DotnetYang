using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class SemanticError(string message, YangStatement source, YangStatement[]? AdditionalSources = null)
    : Exception(message)
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

    public IEnumerable<Location> AdditionalLocations => AdditionalSources is null
        ? []
        : AdditionalSources.Select(
            src => Location.Create(
                src.Metadata.Source,
                new TextSpan(
                    src.Metadata.Position.Offset,
                    src.Metadata.Length
                ),
                new LinePositionSpan(
                    new LinePosition(src.Metadata.Position.Line, src.Metadata.Position.Column),
                    new LinePosition(src.Metadata.Position.Line, src.Metadata.Position.Column + src.Metadata.Length)
                )
            ));
}