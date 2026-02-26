using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Deviation : Statement
{
    public Deviation(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
    }

    private bool Applied;

    public const string Keyword = "deviation";

    public override ChildRule[] PermittedChildren { get; } =
    [
        new ChildRule(Description.Keyword),
        new ChildRule(Deviate.Keyword, Cardinality.OneOrMore),
        new ChildRule(Reference.Keyword),
    ];

    /// <summary>
    /// Applies this deviation to the schema tree.
    /// RFC 7950 §7.20.3: The argument is an absolute schema node identifier.
    /// </summary>
    public void Apply()
    {
        if (Applied) return;
        Applied = true;

        var components = Argument.Split('/');
        var target = ResolveTarget(components);
        if (target is null)
        {
            Log.Write($"Deviation target '{Argument}' could not be resolved, skipping.");
            return;
        }

        foreach (var deviate in Children.OfType<Deviate>())
        {
            deviate.ApplyTo(target);
        }
    }

    private IStatement? ResolveTarget(string[] components)
    {
        // Deviation argument is always an absolute schema node identifier
        var first = components.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        if (first is null)
        {
            Log.Write($"Deviation: empty path '{Argument}'");
            return null;
        }

        var prefix = first.Prefix(out _);
        IStatement? top;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            top = this.GetModule();
        }
        else
        {
            top = this.FindSourceFor(prefix);
        }

        if (top is null)
        {
            Log.Write($"Deviation: could not find module for path '{Argument}'");
            return null;
        }

        var current = top;
        foreach (var xpath in components)
        {
            if (string.IsNullOrWhiteSpace(xpath)) continue;
            xpath.Prefix(out var childName);
            var origin = current;
            foreach (var currentChild in current.Children)
            {
                currentChild.Argument.Prefix(out var trueName);
                if (currentChild is Prefix) continue;
                if (childName != trueName) continue;
                current = currentChild;
                break;
            }

            if (current == origin)
            {
                Log.Write(
                    $"Deviation: could not find '{childName}' in '{current.GetType().Name} {current.Argument}' for path '{Argument}'");
                return null;
            }
        }

        return current;
    }
}
