using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace YangParser.SemanticModel.Builtins;

public class LeafReference() : BuiltinType("leafref", (statement) =>
{
    var path = (Path)statement.Children.First(c => c is Path);
    var target = statement.Parent.Navigate(path.Argument);
    var prefix = path.Argument.Split('/').Last().Prefix(out _);
    var assignation = prefix.Contains('.') ? prefix : prefix + ":";
    if (statement.FindSourceFor(prefix) == target.GetModule())
    {
        prefix = string.Empty;
        assignation = string.Empty;
    }

    var type = target!.GetChild<Type>();
    var bname = target switch
    {
        IXMLReadValue rw => rw.ClassName,
        IXMLParseable ps => ps.ClassName,
        _ => "string"
    };
    if (bname.Contains(":") || bname.Contains("."))
    {
        var p = bname.Prefix(out _);
        if (bname.Contains(":") && !statement.GetModule()!.Usings.ContainsKey(p))
        {
            statement.GetModule()!.Usings[p] = target.GetModule()!.Usings[p];
        }

        return (bname, null);
    }

    if (bname == "string") return (bname, null);
    if (type.Definition is null && !BuiltinTypeReference.IsBuiltinKeyword(type.Argument))
    {
        return (target.ModuleQualifiedClassName(), null);
    }

    if (string.IsNullOrWhiteSpace(prefix) && !BuiltinTypeReference.IsBuiltinKeyword(type.Argument))
    {
        return (target.FullyQualifiedClassName(), null);
    }

    if (BuiltinTypeReference.IsBuiltin(type, out var name, out var def))
    {
        return def is null ? (name!, null) : (target.FullyQualifiedClassName(), null);
    }

    return (assignation + bname, null);
});