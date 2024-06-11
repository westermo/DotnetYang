using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

/// <summary>
/// 9.4.6.  The pattern Statement
///The "pattern" statement, which is an optional substatement to the
///"type" statement, takes as an argument a regular expression string,
///as defined in [XSD-TYPES].  It is used to restrict the built-in type
///"string", or types derived from "string", to values that match the
///pattern.
///
///If the type has multiple "pattern" statements, the expressions are
///AND-ed together, i.e., all such expressions have to match.
///
///    If a pattern restriction is applied to an already pattern-restricted
///type, values must match all patterns in the base type, in addition to
///the new patterns.
///
///9.4.6.1.  The pattern's Substatements
///
///    +---------------+---------+-------------+
///    | substatement  | section | cardinality |
///    +---------------+---------+-------------+
///    | description   | 7.19.3  | 0..1        |
///    | error-app-tag | 7.5.4.2 | 0..1        |
///    | error-message | 7.5.4.1 | 0..1        |
///    | reference     | 7.19.4  | 0..1        |
///    +---------------+---------+-------------+
///
///        9.4.7.  Usage Example
///
///    With the following type:
///
///type string {
///    length "0..4";
///    pattern "[0-9a-fA-F]*";
///}
///
///the following strings match:
///
///AB          // legal
///9A00        // legal
///
///    and the following strings do not match:
///
///00ABAB      // illegal, too long
///    xx00        // illegal, bad characters
/// </summary>
public class Pattern : Statement
{
    public Pattern(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    public const string Keyword = "pattern";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(ErrorAppTag.Keyword),
        new ChildRule(ErrorMessage.Keyword),
        new ChildRule(Modifier.Keyword),
        new ChildRule(Reference.Keyword)
    ];

    public string GetConstructorValidation()
    {
        var hasError = this.TryGetChild<ErrorMessage>(out var errorMessage);
        var hasTag = this.TryGetChild<ErrorAppTag>(out var appTag);
        var invert = Children.FirstOrDefault(c => c is Modifier)?.Argument == "invert-match";
        var message = hasTag || hasError
            ? $"\"{SingleLine(appTag?.Argument ?? "No tag")}: {SingleLine(errorMessage?.Argument ?? string.Empty)}\""
            : invert
                ? $"$\"string \\\"{{input}}\\\" matches pattern \" + @\"{SingleLine(Argument, "")} but is not allowed to\""
                : $"$\"string \\\"{{input}}\\\" does not match pattern \" + @\"{SingleLine(Argument, "")}\"";
        return invert
            ? $"if(Pattern.Match(input).Success) throw new ArgumentException({message});"
            : $"if(!Pattern.Match(input).Success) throw new ArgumentException({message});";
    }

    public string GetDeclaration()
    {
        foreach (var child in Children)
        {
            child.ToCode();
        }

        return $"""
                {DescriptionString}{AttributeString}
                private static Regex Pattern = new Regex(@"{SingleLine(Argument, "")}");
                """;
    }
}

public class Modifier : Statement
{
    public Modifier(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        ValidateChildren(statement);
        if (Argument != "invert-match")
        {
            throw new SemanticError($"Invalid argument for '{statement.Keyword}', expected 'invert-match'", statement);
        }
    }

    public const string Keyword = "modifier";
}