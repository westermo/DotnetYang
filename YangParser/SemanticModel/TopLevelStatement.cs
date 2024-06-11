using System.Collections.Generic;
using System.Linq;
using YangParser.Generator;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public abstract class TopLevelStatement : Statement, ITopLevelStatement
{
    private bool IsExpanded;

    protected TopLevelStatement(YangStatement statement, bool validate = true) : base(statement, validate)
    {
        foreach (var child in this.Unwrap())
        {
            switch (child)
            {
                case Uses use:
                    Uses.Add(use);
                    break;
                case Grouping grouping:
                    Groupings.Add(grouping);
                    break;
                case Augment augment:
                    Augments.Add(augment);
                    break;
                case Import import:
                {
                    Imports.Add(import);
                    var reference = MakeNamespace(import.Argument) + ".YangNode.";
                    var prefix = import.GetChild<Prefix>().Argument;
                    Usings[prefix] = reference;
                    ImportedModules[prefix] = import.Argument;
                    break;
                }
                case TypeDefinition typeDefinition:
                {
                    if (typeDefinition.IsUnderGrouping())
                    {
                        HiddenDefinitions.Add(typeDefinition);
                        typeDefinition.Parent?.Replace(typeDefinition, []);
                    }

                    break;
                }
                case Extension extension:
                    Extensions.Add(extension);
                    break;
                case Feature feature:
                    Features.Add(feature);
                    break;
                case Revision revision:
                    Revisions.Add(revision);
                    break;
                case Identity identity:
                    Identities.Add(identity);
                    break;
                case Rpc rpc:
                    if (!rpc.IsUnderGrouping()) Rpcs.Add(rpc);
                    break;
                case Action action:
                    if (!action.IsUnderGrouping()) Actions.Add(action);
                    break;
                case Notification notification:
                    if (!notification.IsUnderGrouping()) Notifications.Add(notification);
                    break;
            }
        }
    }

    public void Expand()
    {
        if (IsExpanded) return;
        foreach (var child in Children)
        {
            ExpandPrefixes(child);
        }

        IsExpanded = true;
    }

    private void ExpandPrefixes(IStatement statement)
    {
        if (statement is not IUnexpandable)
        {
            if (!statement.Argument.Contains(' ') && !statement.Argument.Contains('(') &&
                !statement.Argument.Contains('['))
                //Only occurs in string arguments, which are unaffected by prefixes, 
                // and in function calls, which are unaffected by prefixes
                // or in regex expressions, which are unaffected by prefixes
            {
                var argPrefix = statement.Argument.Split(':');
                if (argPrefix.Length > 1 &&
                    argPrefix.Length <
                    3) //ignore cases where there are multiple colons, since that's an XML-namespace reference
                {
                    if (Usings.ContainsKey(argPrefix[0]))
                    {
                        statement.Argument = statement.Argument.Replace(argPrefix[0] + ":", Usings[argPrefix[0]]);
                    }
                    else
                    {
                        Log.Write($"No prefix found for {statement.Argument} in module {Argument}");
                    }
                }
            }
        }

        foreach (var child in statement.Children)
        {
            ExpandPrefixes(child);
        }
    }

    public Dictionary<string, string> Usings { get; } = [];
    public List<Uses> Uses { get; } = [];
    public List<Grouping> Groupings { get; } = [];
    public List<Augment> Augments { get; } = [];
    public List<Import> Imports { get; } = [];
    public List<Extension> Extensions { get; } = [];
    public List<Feature> Features { get; } = [];
    public List<Revision> Revisions { get; } = [];
    public List<Identity> Identities { get; } = [];
    public List<Rpc> Rpcs { get; } = [];
    public List<Action> Actions { get; } = [];
    public List<Notification> Notifications { get; } = [];
    public List<TypeDefinition> HiddenDefinitions { get; } = [];
    public Dictionary<string, string> PrefixToNamespaceTable { get; } = [];
    public Dictionary<string, string> ImportedModules { get; } = [];
}