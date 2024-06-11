using System;
using System.Linq;
using YangParser.Parser;

namespace YangParser.SemanticModel;

/// <summary>
/// The "path" statement, which is a substatement to the "type"
/// statement, MUST be present if the type is "leafref".  It takes as an
/// argument a string that MUST refer to a leaf or leaf-list node.
///
/// The syntax for a path argument is a subset of the XPath abbreviated
/// syntax.  Predicates are used only for constraining the values for the
/// key nodes for list entries.  Each predicate consists of exactly one
/// equality test per key, and multiple adjacent predicates MAY be
/// present if a list has multiple keys.  The syntax is formally defined
/// by the rule "path-arg" in Section 12.
///
/// The predicates are only used when more than one key reference is
/// needed to uniquely identify a leaf instance.  This occurs if a list
/// has multiple keys, or a reference to a leaf other than the key in a
/// list is needed.  In these cases, multiple leafrefs are typically
/// specified, and predicates are used to tie them together.
///
/// The "path" expression evaluates to a node set consisting of zero,
/// one, or more nodes.  If the leaf with the leafref type represents
/// configuration data, this node set MUST be non-empty.
///
///The "path" XPath expression is conceptually evaluated in the
///following context, in addition to the definition in Section 6.4.1:
///
///-  The context node is the node in the data tree for which the "path"
///   statement is defined.
///
///The accessible tree depends on the context node:
///
///-  If the context node represents configuration data, the tree is the
///   data in the NETCONF datastore where the context node exists.  The
///   XPath root node has all top-level configuration data nodes in all
///   modules as children.
///
///-  Otherwise, the tree is all state data on the device, and the
///   <running/> datastore.  The XPath root node has all top-level data
///   nodes in all modules as children.
///
/// </summary>
public class Path : Statement
{
    public Path(YangStatement statement) : base(statement)
    {
        if (statement.Keyword != Keyword)
            throw new SemanticError($"Non-matching Keyword '{statement.Keyword}', expected {Keyword}", statement);
        
    }

    public const string Keyword = "path";
}