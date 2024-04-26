using System;
using System.Collections.Generic;
using System.Linq;

namespace YangParser.SemanticModel;

public abstract class Statement : IStatement
{
    protected void ValidateChildren(YangStatement statement)
    {
        Dictionary<string, int> occurances = new();
        foreach (var child in statement.Children)
        {
            if (PermittedChildren.Any(p => p.Keyword == child.Keyword))
            {
                if (occurances.ContainsKey(child.Keyword))
                {
                    occurances[child.Keyword]++;
                }
                else
                {
                    occurances[child.Keyword] = 1;
                }

                continue;
            }

            throw new InvalidOperationException(
                $"Child of type {child.Keyword} is not permitted inside statement of type {GetType()}");
        }

        foreach (var allowed in PermittedChildren)
        {
            switch (allowed.Cardinality)
            {
                case Cardinality.Required when occurances.TryGetValue(allowed.Keyword, out var count):
                    {
                        if (count == 1) break;
                        throw new InvalidOperationException(
                            $"Child of type {allowed.Keyword} can only exist once in {GetType()}");
                    }
                case Cardinality.Required:
                    throw new InvalidOperationException(
                        $"Child of type {allowed.Keyword} must exist in type {GetType()}");
                case Cardinality.ZeroOrOne when occurances.TryGetValue(allowed.Keyword, out var count):
                    {
                        if (count <= 1) break;
                        throw new InvalidOperationException(
                            $"Child of type {allowed.Keyword} can only exist up to once in {GetType()}");
                    }
                case Cardinality.ZeroOrOne:
                case Cardinality.ZeroOrMore:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(allowed.Cardinality.ToString());
            }
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string Argument { get; protected set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public virtual ChildRule[] PermittedChildren { get; } = [];
    public IStatement[] Children { get; protected set; } = [];
}