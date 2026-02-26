using System;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

///<summary>
/// The "deviate" statement defines how the device's implementation of
///    the target node deviates from its original definition.  The argument
///    is one of the strings "not-supported", "add", "replace", or "delete".
///
///    The argument "not-supported" indicates that the target node is not
///    implemented by this device.
///
///    The argument "add" adds properties to the target node.  The
///    properties to add are identified by substatements to the "deviate"
///    statement.  If a property can only appear once, the property MUST NOT
///    exist in the target node.
///
///    The argument "replace" replaces properties of the target node.  The
///    properties to replace are identified by substatements to the
///    "deviate" statement.  The properties to replace MUST exist in the
///    target node.
///
///    The argument "delete" deletes properties from the target node.  The
///    properties to delete are identified by substatements to the "delete"
///    statement.  The substatement's keyword MUST match a corresponding
///    keyword in the target node, and the argument's string MUST be equal
///    to the corresponding keyword's argument string in the target node.
///
///                        The deviates's Substatements
///
///                  +--------------+---------+-------------+
///                  | substatement | section | cardinality |
///                  +--------------+---------+-------------+
///                  | config       | 7.19.1  | 0..1        |
///                  | default      | 7.6.4   | 0..1        |
///                  | mandatory    | 7.6.5   | 0..1        |
///                  | max-elements | 7.7.4   | 0..1        |
///                  | min-elements | 7.7.3   | 0..1        |
///                  | must         | 7.5.3   | 0..n        |
///                  | type         | 7.4     | 0..1        |
///                  | unique       | 7.8.3   | 0..n        |
///                  | units        | 7.3.3   | 0..1        |
///                  +--------------+---------+-------------+
/// </summary>
public class Deviate : Statement
{
    public Deviate(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);

        Children = statement.Children.Select(StatementFactory.Create).ToArray();
        switch (Argument)
        {
            case "not-supported":
            case "add":
            case "replace":
            case "delete":
                break;
            default:
                throw new InvalidOperationException($"Invalid argument '{Argument}' for keyword '{Keyword}'");
        }

        ValidateChildren(statement);
    }

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Config.Keyword),
        new ChildRule(DefaultValue.Keyword),
        new ChildRule(Mandatory.Keyword),
        new ChildRule(MaxElements.Keyword),
        new ChildRule(MinElements.Keyword),
        new ChildRule(Must.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Type.Keyword),
        new ChildRule(Unique.Keyword, Cardinality.ZeroOrMore),
        new ChildRule(Units.Keyword),
    ];

    public const string Keyword = "deviate";

    /// <summary>
    /// Applies this deviate operation to the given target node.
    /// RFC 7950 §7.20.3.2-5
    /// </summary>
    public void ApplyTo(IStatement target)
    {
        switch (Argument)
        {
            case "not-supported":
                ApplyNotSupported(target);
                break;
            case "add":
                ApplyAdd(target);
                break;
            case "replace":
                ApplyReplace(target);
                break;
            case "delete":
                ApplyDelete(target);
                break;
        }
    }

    /// <summary>
    /// RFC 7950 §7.20.3.2: Remove the target node entirely.
    /// </summary>
    private static void ApplyNotSupported(IStatement target)
    {
        if (target.Parent is null)
        {
            Log.Write($"Deviate not-supported: target '{target.Argument}' has no parent, cannot remove.");
            return;
        }

        target.Parent.Replace(target, []);
    }

    /// <summary>
    /// RFC 7950 §7.20.3.3: Add properties to the target node.
    /// Singleton properties must not already exist on the target.
    /// </summary>
    private void ApplyAdd(IStatement target)
    {
        foreach (var child in Children)
        {
            if (child is Description or Reference) continue; // These are about the deviate itself
            target.Insert([child]);
        }
    }

    /// <summary>
    /// RFC 7950 §7.20.3.4: Replace properties of the target node.
    /// The properties to replace must already exist on the target.
    /// </summary>
    private void ApplyReplace(IStatement target)
    {
        foreach (var child in Children)
        {
            if (child is Description or Reference) continue;

            var childType = child.GetType();
            var existing = target.Children.FirstOrDefault(c => c.GetType() == childType);
            if (existing is not null)
            {
                target.Replace(existing, [child]);
                child.Parent = target;
            }
            else
            {
                Log.Write(
                    $"Deviate replace: property '{child.GetType().Name}' not found on target '{target.Argument}', inserting instead.");
                target.Insert([child]);
            }
        }
    }

    /// <summary>
    /// RFC 7950 §7.20.3.5: Delete properties from the target node.
    /// The substatement keyword must match, and argument must be equal.
    /// </summary>
    private void ApplyDelete(IStatement target)
    {
        foreach (var child in Children)
        {
            if (child is Description or Reference) continue;

            var childType = child.GetType();
            // For delete, we match by type AND argument value
            var existing = target.Children.FirstOrDefault(c =>
                c.GetType() == childType && c.Argument == child.Argument);
            if (existing is not null)
            {
                target.Replace(existing, []);
            }
            else
            {
                Log.Write(
                    $"Deviate delete: property '{child.GetType().Name} {child.Argument}' not found on target '{target.Argument}'.");
            }
        }
    }
}