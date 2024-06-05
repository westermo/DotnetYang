using System.Collections.Generic;

namespace YangParser.SemanticModel;

public interface ITopLevelStatement : IStatement
{
    
    public Dictionary<string, string> Usings { get; }
    public List<Uses> Uses { get; } 
    public List<Grouping> Groupings { get; }
    public List<Augment> Augments { get; }
    public List<Import> Imports { get; } 
    public List<Extension> Extensions { get; } 
    public List<Feature> Features { get; }
    public List<Revision> Revisions { get; } 
    public List<Identity> Identities { get; }
    public List<Rpc> Rpcs { get; }
    public List<Action> Actions { get; } 
    public List<Notification> Notifications { get; } 
    public List<TypeDefinition> HiddenDefinitions { get; } 
    public Dictionary<string, string> PrefixToNamespaceTable { get; }
    public Dictionary<string, string> ImportedModules { get; }
    public void Expand();
}