using System;
using System.Collections.Generic;

namespace YangSupport;

/// <summary>
/// Thrown by generated <c>Validate()</c> methods when a YANG <c>must</c>
/// constraint is violated, when a <c>when</c> condition forbids a node that is
/// nevertheless present, or when min/max-elements / unique constraints fail.
/// </summary>
public sealed class YangValidationException : Exception
{
    public YangValidationException(string message, string? schemaPath = null, string? expression = null,
        string? errorAppTag = null, string? errorMessage = null)
        : base(message)
    {
        SchemaPath = schemaPath;
        Expression = expression;
        ErrorAppTag = errorAppTag;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Slash-delimited YANG schema path of the node whose constraint was violated.
    /// </summary>
    public string? SchemaPath { get; }

    /// <summary>
    /// The original XPath expression (when/must) that failed, if applicable.
    /// Kept for diagnostics; the runtime check itself is compiled code.
    /// </summary>
    public string? Expression { get; }

    /// <summary>
    /// YANG <c>error-app-tag</c> value associated with the constraint.
    /// </summary>
    public string? ErrorAppTag { get; }

    /// <summary>
    /// YANG <c>error-message</c> value associated with the constraint.
    /// </summary>
    public string? ErrorMessage { get; }
}

/// <summary>
/// Aggregates multiple validation failures encountered during a single
/// <c>Validate()</c> pass over a YANG tree.
/// </summary>
public sealed class YangValidationAggregateException : Exception
{
    public YangValidationAggregateException(IReadOnlyList<YangValidationException> failures)
        : base($"{failures.Count} YANG validation failures")
    {
        Failures = failures;
    }

    public IReadOnlyList<YangValidationException> Failures { get; }
}
