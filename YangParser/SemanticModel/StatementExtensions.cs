using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Generator;

namespace YangParser.SemanticModel;

public static class StatementExtensions
{
    public static IStatement Root(this IStatement statement)
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

    public static bool TryGetChild<T>(this IStatement statement, out T? child) where T : class, IStatement
    {
        child = null;
        if (statement.Children.FirstOrDefault(c => c is T) is T chosen)
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

    public static IStatement? FindSourceFor(this IStatement source, string prefix)
    {
        var module = source.GetModule();
        var imports = module?.Imports;
        return imports?.FirstOrDefault(import => import.GetChild<Prefix>().Argument == prefix);
    }

    public static Grouping? FindGrouping(this Uses source, ITopLevelStatement module)
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

    public static Grouping GetGrouping(this Uses use)
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
                var import = use.FindSourceFor(prefix);
                if (import is null)
                {
                    throw new SemanticError(
                        $"Could not find an import statement with prefix '{prefix}' for 'uses {use.Argument}' in module '{module.Argument}'",
                        use.Source);
                }

                var source = use.Root().Children.OfType<Module>().FirstOrDefault(m => m.Argument == import.Argument) ??
                             throw new SemanticError($"Could not find a module with the key {import.Argument}",
                                 import.Source);
                var grouping = use.FindGrouping(source) ?? throw new SemanticError(
                    $"Could not find a grouping statement to use for 'uses {use.Argument}' in module '{source.Argument}' from prefix '{prefix}', import was {Statement.SingleLine(import.ToString())}",
                    use.Source);
                return grouping;
            }
            else
            {
                var source = use.Root().Children.OfType<Module>().FirstOrDefault(m => m.Namespace == prefix) ??
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
        else if (argument.Contains("."))
        {
            var components = argument.Split('.');
            StringBuilder builder = new();
            for (int i = 0; i < components.Length - 1; i++)
            {
                builder.Append(components[i]);
                builder.Append(".");
            }

            name = components.Last();
            return builder.ToString();
        }

        name = argument;
        return string.Empty;
    }

    static Dictionary<(System.Type, string), IStatement?> _cache = [];

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
            else if (!prefix.Contains('.'))
            {
                IStatement? module;
                var import = source.FindSourceFor(prefix);
                var moduleName = import?.Argument;
                if (import is null)
                {
                    Log.Write(
                        $"Failed to find import for '{reference}' from module '{source.GetModule()?.Argument}', available imports are {string.Join(",", source.GetModule()?.Unwrap().OfType<Import>().Select(i => $"[{i.GetChild<Prefix>().Argument} -> {i.Argument}]") ?? [])}");
                    return default;
                }

                module = source.Root().Children.FirstOrDefault(c => c.Argument == moduleName);
                if (module is null)
                {
                    Log.Write(
                        $"Failed to find module for '{moduleName}'");
                    return default;
                }

                var value = module.SearchDownwards<T>(name) ?? module.SearchUpwards<T>(name);
                _cache[key] = value;
                return value;
            }
            else
            {
                var module = source.Root().Children.OfType<Module>().FirstOrDefault(m => m.Namespace == prefix);
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
        if (source.Argument == argument && source is T t && source is not DefaultValue)
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

    public static T? SearchUpwards<T>(this IStatement source, string argument) where T : IStatement
    {
        if (source.Argument == argument && source is T t && source is not DefaultValue)
        {
            return t;
        }

        if (source.Parent is null) return default;
        var result = SearchDownwards<T>(source.Parent, argument, source);
        if (result is not null) return result;
        return source.Parent.SearchUpwards<T>(argument);
    }
}