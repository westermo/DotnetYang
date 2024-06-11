using System.Collections.Generic;
using System.Text;
using YangParser.Parser;

namespace YangParser.SemanticModel;

public abstract class NodeDataStatement(YangStatement statement) : Statement(statement), IXMLParseable
{
    private string? _targetPath;
    public string TargetPath => _targetPath ??= GetTargetPath();

    private string GetTargetPath()
    {
        StringBuilder entries = new StringBuilder();
        entries.Append(TargetName);
        var parent = Parent;
        while (parent is not null)
        {
            if (parent is Module) break;
            if (parent is IXMLParseable parseable)
            {
                entries.Insert(0, parseable.TargetName! + "?.");
            }
            else if (parent is List list)
            {
                var content = entries.ToString();
                entries.Insert(0,
                    list.TargetName +
                    $"?.FirstOrDefault({list.ClassName.ToLower()} => {list.ClassName.ToLower()}?.{content} != null)?.");
            }
            else
            {
                throw new SemanticError(
                    $"Could not describe full target path of action {Argument}: encountered unknown {parent.GetType().Name} {parent.Argument}",
                    parent.Source);
            }

            parent = parent.Parent;
        }

        return entries.ToString();
    }

    private IXMLParseable? _root;
    public IXMLParseable Root => _root ??= QualifiedRoot();

    private IXMLParseable QualifiedRoot()
    {
        if (Parent is Module module) return this;
        var parent = Parent;
        while (parent is not Module && parent is not null)
        {
            if (parent.Parent is Module)
            {
                if (parent is not IXMLParseable parseable)
                {
                    throw new SemanticError(
                        $"Action {Argument}: qualified root '{parent.GetType().Name} {parent.Argument}' was not Parseable or readable",
                        Source);
                }

                return parseable;
            }

            parent = parent.Parent;
        }

        if (parent is null or Module)
        {
            throw new SemanticError($"Action {Argument}: qualified root was null or a module", Source);
        }

        if (parent is not IXMLParseable xmlParseable)
        {
            throw new SemanticError(
                $"Action {Argument}: qualified root '{parent.GetType().Name} {parent.Argument}' was not Parseable or readable",
                Source);
        }

        return xmlParseable;
    }

    private string? _rootName;

    public string QualifiedRootName =>
        _rootName ??=
            MakeNamespace(Root.GetModule()!.Argument) + ".YangNode." +
            Root.ClassName;

    public string FullyQualifiedNamespace()
    {
        var parent = Parent;
        List<string> classChain = new();
        while (parent is not Module && parent is not null)
        {
            switch (parent)
            {
                case IXMLParseable xml:
                    classChain.Insert(0, xml.ClassName);
                    break;
                case IXMLReadValue readValue:
                    classChain.Insert(0, readValue.ClassName);
                    break;
            }

            parent = parent.Parent;
        }

        if (parent is Module module)
        {
            classChain.Insert(0, "YangNode");
            classChain.Insert(0, MakeNamespace(module.Argument));
        }

        return string.Join(".", classChain);
    }

    public abstract string? TargetName { get; }
    public abstract string ClassName { get; }
}