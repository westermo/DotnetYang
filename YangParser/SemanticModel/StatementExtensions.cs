using System.Collections.Generic;
using System.Linq;
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

    public static IStatement? FindSourceFor(this IStatement source, string prefix)
    {
        var module = source.Root();
        var imports = module?.Unwrap().OfType<Import>();
        return imports?.FirstOrDefault(import => import.GetChild<Prefix>().Argument == prefix);
    }

    public static Grouping? FindGrouping(this Uses source, IStatement module)
    {
        var key = source.Argument.Split(':').Last();
        foreach (var child in module.Unwrap())
        {
            if (child is not Grouping grouping) continue;
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

        if (use.Argument.Contains(":")) //Is 'outside-of-tree'
        {
            var components = use.Argument.Split(':');
            var prefix = components.First();
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
            else
            {
                var import = use.FindSourceFor(prefix);
                if (import is null)
                {
                    throw new SemanticError(
                        $"Could not find an import statement with prefix {prefix} for 'uses {use.Argument}' in module '{module.Argument}'",
                        use.Source);
                }

                var source = use.Root().Children.OfType<Module>()
                    .FirstOrDefault(m => m.Argument == import.Argument);
                if (source is null)
                {
                    throw new SemanticError($"Could not find a module with the key {import.Argument}", import.Source);
                }

                var grouping = use.FindGrouping(source);

                if (grouping is null)
                {
                    throw new SemanticError(
                        $"Could not find a grouping statement to use for 'uses {use.Argument}' in module '{source.Argument}' from prefix '{prefix}', import was {Statement.SingleLine(import.ToString())}",
                        use.Source);
                }

                return grouping;
            }
        }

        var findGrouping = use.FindGrouping(module);
        if (findGrouping is null)
        {
            throw new SemanticError(
                $"Could not find a grouping statement to use for 'uses {use.Argument}' in module '{module.Argument}'",
                use.Source);
        }

        return findGrouping;
    }


    public static void Expand(this Uses use)
    {
        var grouping = use.GetGrouping();
        var parent = use.Parent;
        parent!.Replace(use, grouping.WithUse(use));
    }

    public static IStatement? FindReference(this IStatement source, string reference)
    {
        var components = reference.Split(':');
        IStatement? module;
        string name;
        if (components.Length == 1)
        {
            module = source.GetModule();
            name = components[0];
        }
        else
        {
            var prefix = components[0];
            if (source.GetInheritedPrefix() == prefix)
            {
                module = source.GetModule();
                name = components[1];
            }
            else
            {
                var import = source.FindSourceFor(prefix);
                var moduleName = import?.Argument;
                if (import is null)
                {
                    Log.Write(
                        $"Failed to find import for '{reference}' from module '{source.GetModule()?.Argument}', available imports are {string.Join(",", source.GetModule()?.Unwrap().OfType<Import>().Select(i => $"[{i.GetChild<Prefix>().Argument} -> {i.Argument}]") ?? [])}");
                    return null;
                }

                module = source.Root().Children.FirstOrDefault(c => c.Argument == moduleName);
                if (module is null)
                {
                    Log.Write(
                        $"Failed to find module for '{moduleName}'");
                    return null;
                }

                name = components[1];
            }
        }


        var value = module?.Unwrap().FirstOrDefault(c => c.Argument == name && c is not DefaultValue && c != source);
        value ??= source.Root().Unwrap()
            .FirstOrDefault(c => c.Argument == name && c is not DefaultValue && c != source);
        return value;
    }
}