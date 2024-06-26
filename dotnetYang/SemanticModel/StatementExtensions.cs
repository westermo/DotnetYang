using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YangParser.Generator;
using YangParser.SemanticModel.Builtins;

namespace YangParser.SemanticModel;

public static class StatementExtensions
{
    public static string FullyQualifiedClassName(this IStatement? statement)
    {
        List<string> classChain = new();
        while (statement is not Module && statement is not null)
        {
            switch (statement)
            {
                case IXMLParseable xml:
                    xml.ClassName.Prefix(out var name);
                    classChain.Insert(0, name);
                    break;
                case IXMLReadValue readValue:
                    readValue.ClassName.Prefix(out var rname);
                    classChain.Insert(0, rname);
                    break;
            }

            statement = statement.Parent;
        }

        if (statement is Module module)
        {
            classChain.Insert(0, "YangNode");
            classChain.Insert(0, Statement.MakeNamespace(module.Argument));
        }

        return string.Join(".", classChain);
    }

    public static string ModuleQualifiedClassName(this IStatement? statement)
    {
        List<string> classChain = [];
        if (statement is IXMLReadValue rw) classChain.Add(rw.ClassName);
        else if (statement is IXMLParseable parseable) classChain.Add(parseable.ClassName);
        while (statement is not Module && statement is not null)
        {
            statement = statement.Parent;
        }

        if (statement is Module module)
        {
            classChain.Insert(0, "YangNode");
            classChain.Insert(0, Statement.MakeNamespace(module.Argument));
        }

        return string.Join(".", classChain);
    }

    public static IStatement? Navigate(this IStatement? context, string xpath)
    {
        xpath = Regex.Replace(xpath, @"\[.*?]", string.Empty);
        if (xpath.StartsWith("/")) context = context.GetModule();
        var components = xpath.Split('/');
        foreach (var path in components)
        {
            var start = context;
            var prefix = path.Prefix(out var truePath);
            if (context is Module && !string.IsNullOrWhiteSpace(prefix))
            {
                context = context.FindSourceFor(prefix);
                if (context is null)
                {
                    Log.Write($"No module found for '{prefix}' at '{truePath}' in {xpath}, bailing");
                    return context;
                }
            }

            if (string.IsNullOrWhiteSpace(path)) continue;
            if (path == "..")
            {
                context = context?.Parent;
                while (context is Case or Choice or Input or Output) context = context.Parent;
                if (context is null)
                {
                    Log.Write($"Parent null at '{path}' in {xpath}, bailing");
                    return context;
                }

                continue;
            }

            var children = context!.Children;
            context = LocateChild(children, truePath);
            if (context is null)
            {
                Log.Write(
                    $"Null at '{truePath}' in {xpath} in '{start?.GetType().Name} {start?.Argument}', children: [{string.Join(", ", children.Where(c => c is IXMLParseable or IXMLReadValue).Select(c => $"{c.GetType().Name} \"{c.Argument}\""))}]");
                return context;
            }
        }

        return context;
    }

    public static IStatement? LocateChild(IEnumerable<IStatement> children, string truePath)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case Choice:
                case Case:
                case Output:
                case Input:
                    var potential = LocateChild(child.Children, truePath);
                    if (potential is not null) return potential;
                    break;
                case IXMLParseable or IXMLReadValue when child.Argument == truePath:
                    return child;
            }
        }

        return null;
    }

    public static IStatement? LocateChildWithInvisibleAllowed(IEnumerable<IStatement> children, string truePath)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case Choice:
                case Case:
                case Output:
                case Input:
                    if (child.Argument == truePath) return child;
                    var potential = LocateChild(child.Children, truePath);
                    if (potential is not null) return potential;
                    break;
                case IXMLParseable or IXMLReadValue when child.Argument == truePath:
                    return child;
            }
        }

        return null;
    }

    private static IStatement Root(this IStatement statement)
    {
        while (statement.Parent is not null)
        {
            statement = statement.Parent;
        }

        return statement;
    }

    public static string GetInheritedPrefix(this IStatement? statement)
    {
        while (statement is not Module or null)
        {
            statement = statement?.Parent;
        }

        if (statement is Module module)
        {
            return module.GetChild<Prefix>().Argument;
        }

        return string.Empty;
    }

    public static T GetChild<T>(this IStatement statement) where T : class, IStatement
    {
        return (T)statement.Children.First(c => c is T);
    }

    public static bool TryGetChild<T>(this IStatement? statement, out T? child) where T : class, IStatement
    {
        child = null;
        if (statement?.Children.FirstOrDefault(c => c is T) is T chosen)
        {
            child = chosen;
            return true;
        }

        return false;
    }

    public static IEnumerable<IStatement> Unwrap(this IStatement source)
    {
        yield return source;
        foreach (var child in source.Children)
        {
            foreach (var subChild in Unwrap(child))
            {
                yield return subChild;
            }
        }
    }

    public static ITopLevelStatement? GetModule(this IStatement? source)
    {
        while (source is not null)
        {
            source = source.Parent;
            switch (source)
            {
                case Submodule submodule:
                    return submodule;
                case Module module:
                    return module;
            }
        }

        return null;
    }

    public static ITopLevelStatement? FindSourceFor(this IStatement source, string prefix)
    {
        var module = source as Module ?? source.GetModule();
        if (module?.ImportedModules.TryGetValue(prefix, out var moduleName) == true)
        {
            return module.Root().Children.OfType<Module>().FirstOrDefault(s => s.Argument == moduleName);
        }

        return null;
    }

    private static Grouping? FindGrouping(this Uses source, ITopLevelStatement module)
    {
        _ = source.Argument.Prefix(out var key);
        foreach (var grouping in module.Groupings)
        {
            if (grouping.Argument == key)
            {
                return grouping;
            }
        }

        return null;
    }

    public static bool IsUnderGrouping(this IStatement? statement)
    {
        while (statement is not null)
        {
            if (statement.Parent is Grouping) return true;
            statement = statement.Parent;
        }

        return false;
    }

    private static Grouping GetGrouping(this Uses use)
    {
        var module = use.GetModule();
        if (module is null)
        {
            throw new SemanticError(
                $"Could not find a module for 'uses {use.Argument}'",
                use.Source);
        }

        var prefix = use.Argument.Prefix(out _);
        if (!string.IsNullOrEmpty(prefix)) //Is 'outside-of-tree'
        {
            if (prefix == use.GetInheritedPrefix())
            {
                var grouping = use.FindGrouping(module);
                if (grouping is null)
                {
                    throw new SemanticError(
                        $"Could not find a grouping statement to use for 'uses {use.Argument}' in self module '{module.Argument}'",
                        use.Source);
                }

                return grouping;
            }

            if (!prefix.Contains("."))
            {
                var source = use.FindSourceFor(prefix) ??
                             throw new SemanticError($"Could not find a module with the key {prefix}", use.Source);
                var grouping = use.FindGrouping(source) ?? throw new SemanticError(
                    $"Could not find a grouping statement to use for 'uses {use.Argument}' in module '{source.Argument}' from prefix '{prefix}'",
                    use.Source);
                return grouping;
            }
            else
            {
                var source = use.Root().Children.OfType<Module>().FirstOrDefault(m => m.MyNamespace == prefix) ??
                             throw new SemanticError($"Could not find a module with the key {prefix}", use.Source);
                var grouping = use.FindGrouping(source) ?? throw new SemanticError(
                    $"Could not find a grouping statement to use for 'uses {use.Argument}' in module '{source.Argument}' from prefix '{prefix}'",
                    use.Source);
                return grouping;
            }
        }

        var findGrouping = use.FindGrouping(module);
        if (findGrouping is null)
        {
            throw new SemanticError(
                $"Could not find a grouping statement to use for 'uses {use.Argument}' [{use.Parent}] in module '{module.Argument}'",
                use.Source);
        }

        return findGrouping;
    }


    public static void Expand(this Uses use)
    {
        if (use.Parent?.Children.Contains(use) != true) return;
        var grouping = use.GetGrouping();
        var parent = use.Parent;
        parent!.Replace(use, grouping.WithUse(use));
    }

    public static string Prefix(this string argument, out string name)
    {
        if (argument.Contains(":"))
        {
            var components = argument.Split(':');
            name = components[1];
            return components[0];
        }

        if (argument.Contains("."))
        {
            var versioning = Statement.VersionIndicator.Match(argument);
            if (versioning.Success)
            {
                var component = versioning.Groups["target"].Value;
                var replacement = component.Replace(".", "[tmp]");
                argument = argument.Replace(component, replacement);
            }

            var components = argument.Split('.');
            StringBuilder builder = new();
            for (int i = 0; i < components.Length - 1; i++)
            {
                builder.Append(components[i]);
                builder.Append(".");
            }

            name = components.Last().Replace("[tmp]", ".");
            return builder.ToString();
        }

        name = argument;
        return string.Empty;
    }

    static readonly Dictionary<(System.Type, string), IStatement?> _cache = [];

    public static T? FindReference<T>(this IStatement source, string reference) where T : IStatement
    {
        var key = (typeof(T), reference);
        if (_cache.TryGetValue(key, out var cached))
        {
            return (T?)cached;
        }

        var prefix = reference.Prefix(out var name);
        if (string.IsNullOrEmpty(prefix))
        {
            var value = source.SearchDownwards<T>(name) ?? source.SearchUpwards<T>(name);
            _cache[key] = value;
            return value;
        }
        else
        {
            if (source.GetInheritedPrefix() == prefix)
            {
                var value = source.SearchDownwards<T>(name) ?? source.SearchUpwards<T>(name);
                _cache[key] = value;
                return value;
            }

            if (!prefix.Contains('.'))
            {
                var module = source.FindSourceFor(prefix);
                if (module is null)
                {
                    Log.Write(
                        $"Failed to find module for '{prefix}'");
                    return default;
                }

                var value = module.SearchDownwards<T>(name) ?? module.SearchUpwards<T>(name);
                _cache[key] = value;
                return value;
            }
            else
            {
                var module = source.Root().Children.OfType<Module>().FirstOrDefault(m => m.MyNamespace == prefix);
                if (module is null)
                {
                    Log.Write(
                        $"Failed to find module for '{prefix}'");
                    return default;
                }

                var value = module.SearchDownwards<T>(name) ?? module.SearchUpwards<T>(name);
                _cache[key] = value;
                return value;
            }
        }
    }

    public static T? Ancestor<T>(this IStatement source) where T : IStatement
    {
        while (source.Parent is not null)
        {
            source = source.Parent;
            if (source is T t)
            {
                return t;
            }
        }

        return default;
    }

    public static T? SearchDownwards<T>(this IStatement source, string argument, params IStatement[] except)
        where T : IStatement
    {
        if (source.Argument == argument && source is T t and not DefaultValue)
        {
            return t;
        }

        foreach (var child in source.Children.Except(except))
        {
            var result = SearchDownwards<T>(child, argument);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }

    private static T? SearchUpwards<T>(this IStatement source, string argument) where T : IStatement
    {
        if (source.Argument == argument && source is T t and not DefaultValue)
        {
            return t;
        }

        if (source.Parent is null) return default;
        var result = SearchDownwards<T>(source.Parent, argument, source);
        if (result is not null) return result;
        return source.Parent.SearchUpwards<T>(argument);
    }

    public static string GetBaseType(this Type type, out string prefix, out Type chosenType)
    {
        prefix = string.Empty;
        while (true)
        {
            if (BuiltinTypeReference.IsBuiltinKeyword(type.Argument))
            {
                if (type.Argument == "leafref")
                {
                    var path = (Path)type.Children.First(c => c is Path);
                    var target = type.Parent.Navigate(path.Argument);
                    if (target.TryGetChild<Type>(out var targetType))
                    {
                        type = targetType!;
                        continue;
                    }
                }

                chosenType = type;
                _ = type.Argument.Prefix(out var arg);
                return arg;
            }

            var newPrefix = type.Argument.Prefix(out _);
            if (!string.IsNullOrEmpty(newPrefix)) prefix = newPrefix;

            var source = type.FindReference<TypeDefinition>(type.Argument);
            if (source is null)
            {
                throw new SemanticError($"Could not find source for type definition {type.Argument}", type.Source);
            }

            type = source.GetChild<Type>();
        }
    }
}