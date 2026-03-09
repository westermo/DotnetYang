using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public class Status : Statement, IAttributeSource
{
    public Status(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
        ValidateChildren(statement);
        switch (Argument)
        {
            case "current":
                break;
            case "deprecated":
                Active = true;
                IsObsolete = false;
                break;
            case "obsolete":
                Active = true;
                IsObsolete = true;
                break;
            default:
                throw new InvalidOperationException($"Invalid {Keyword} value '{Argument}'");
        }
    }

    public const string Keyword = "status";
    public string AttributeName => IsObsolete 
        ? "Obsolete(\"This node is OBSOLETE per the YANG model and should not be used.\")" 
        : "Obsolete(\"This node is DEPRECATED per the YANG model.\")";
    public bool Active { get; }
    private bool IsObsolete { get; }

    public override string ToCode()
    {
        if (Active)
        {
            Parent?.Attributes.Add(AttributeName);
        }

        return string.Empty;
    }
}