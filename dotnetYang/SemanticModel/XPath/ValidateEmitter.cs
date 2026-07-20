// Emits a public void Validate() method body for generated YANG node classes.
//
// The emitter walks the schema children of the given statement, gathering
// `when` and `must` sub-statements and producing C# checks via XPathTranslator.
// Untranslatable XPath expressions are degraded to a documentation comment so
// the build does not fail; the original XPath text appears only in comments.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using YangParser.Generator;

namespace YangParser.SemanticModel.XPath;

internal static class ValidateEmitter
{
    /// <summary>
    /// Generate the YangValidate() method for a YANG node that maps to a class
    /// (Container, List entry, Choice, Case, Module). Returns a method that
    /// evaluates all when/must constraints attached to this node and to its
    /// leaf and leaf-list children, then recurses into class-producing children.
    /// </summary>
    public static string EmitValidateMethod(IStatement schema)
    {
        var body = new StringBuilder();

        // Emit when/must on the container/list/choice/case node itself.
        EmitNodeConstraints(body, schema, schema);

        // For each leaf / leaf-list child, emit their when/must inline.
        foreach (var child in schema.Children)
        {
            if (child is Leaf leaf)
            {
                EmitNodeConstraints(body, schema, leaf);
                EmitInstanceIdentifierCheck(body, leaf);
            }
            else if (child is LeafList ll)
            {
                EmitNodeConstraints(body, schema, ll);
            }
        }

        // Recurse into class-producing children.
        foreach (var child in schema.Children)
        {
            EmitChildRecursion(body, child);
        }

        var bodyText = body.ToString();
        return $$"""
                 public void YangValidate()
                 {
                     {{Statement.Indent(bodyText)}}
                 }
                 """;
    }

    /// <summary>
    /// Emit when/must constraints for a given schema node. The <paramref name="enclosingClass"/>
    /// is the container/list that owns the Validate() body (determines `this` in the
    /// generated code).
    /// </summary>
    private static void EmitNodeConstraints(StringBuilder body, IStatement enclosingClass, IStatement schema)
    {
        foreach (var when in schema.Children.OfType<When>())
        {
            // Determine correct XPath context:
            IStatement context;
            if (when.OriginalContext is not null)
            {
                // From an augment — use the recorded target.
                context = when.OriginalContext;
            }
            else
            {
                // when authored directly on the node: context IS the node.
                context = schema;
            }

            var (selfExpr, enclosing) = BuildContextExpressions(enclosingClass, context);
            if (selfExpr is null)
            {
                Generator.Log.Write(
                    $"Untranslatable when XPath at {schema.XPath}: " +
                    "Cannot compute YangParent navigation to augment's original context.");
                body.AppendLine($"// when: {OneLine(when.Argument)}");
                body.AppendLine("// (Context navigation not possible; constraint not enforced.)");
                continue;
            }
            var translator = new XPathTranslator(context, selfExpr, enclosing);
            EmitConstraint(body, translator, schema, when.Argument, "when", null, null);
        }
        foreach (var must in schema.Children.OfType<Must>())
        {
            // must: context is the node itself. For leaves, self-expression
            // is the leaf property value; for containers, it's `this`.
            XPathTranslator translator;
            if (schema is Leaf leaf)
            {
                translator = new XPathTranslator(leaf, $"(this.{leaf.TargetName})", "this");
            }
            else if (schema is LeafList ll)
            {
                translator = new XPathTranslator(ll, $"(this.{ll.TargetName})", "this");
            }
            else
            {
                translator = new XPathTranslator(schema, "this", "this");
            }
            string? appTag = null;
            string? msg = null;
            if (must.TryGetChild<ErrorAppTag>(out var et)) appTag = et!.Argument;
            if (must.TryGetChild<ErrorMessage>(out var em)) msg = em!.Argument;
            EmitConstraint(body, translator, schema, must.Argument, "must", appTag, msg);
        }
    }

    /// <summary>
    /// Build a C# self/enclosing expression pair that navigates from the
    /// enclosingClass (the Validate() body's 'this') up to the intended
    /// XPath context node via YangParent hops.
    /// </summary>
    private static (string selfExpr, string enclosingThis) BuildContextExpressions(
        IStatement enclosingClass, IStatement context)
    {
        if (context == enclosingClass)
        {
            return ("this", "this");
        }

        // Special case: context is a direct child (leaf/leaf-list) of the
        // enclosingClass. The self-expression is the property access on 'this'.
        if (context.Parent == enclosingClass || IsChildOf(context, enclosingClass))
        {
            if (context is Leaf leaf)
            {
                return ($"(this.{leaf.TargetName})", "this");
            }
            if (context is LeafList ll)
            {
                return ($"(this.{ll.TargetName})", "this");
            }
        }

        // Walk from enclosingClass up to context via Parent links.
        var hops = 0;
        var cur = (IStatement?)enclosingClass;
        bool found = false;
        while (cur is not null)
        {
            cur = cur.Parent;
            if (cur is null) break;

            var isAddressable = Statement.ResolveQualifiedClassName(cur) is not null;
            if (isAddressable)
            {
                hops++;
            }

            if (cur == context)
            {
                if (!isAddressable) hops++;
                found = true;
                break;
            }
        }

        if (!found)
        {
            return (null!, null!);
        }

        var contextType = Statement.ResolveQualifiedClassName(context);
        if (contextType is null) return (null!, null!);

        string expr = "this";
        for (int i = 0; i < hops; i++)
        {
            expr = $"(({expr}).YangParent)";
        }
        expr = $"(({contextType})({expr})!)";
        return (expr, expr);
    }

    private static bool IsChildOf(IStatement node, IStatement potentialParent)
    {
        var cur = node.Parent;
        while (cur is not null)
        {
            if (cur == potentialParent) return true;
            cur = cur.Parent;
        }
        return false;
    }

    private static void EmitChildRecursion(StringBuilder body, IStatement child)
    {
        switch (child)
        {
            case Container c:
                body.AppendLine($"{c.TargetName}?.YangValidate();");
                break;
            case List l:
                EmitMinMaxElementsCheck(body, l, l.TargetName);
                EmitUniqueConstraintCheck(body, l);
                body.AppendLine($"if ({l.TargetName} is not null) foreach (var __entry in {l.TargetName}) __entry.YangValidate();");
                break;
            case LeafList ll:
                EmitMinMaxElementsCheck(body, ll, ll.TargetName);
                break;
            case Choice ch:
                body.AppendLine($"{Statement.MakeName(ch.Argument)}?.YangValidate();");
                break;
            case Case cs:
                body.AppendLine($"{cs.TargetName}?.YangValidate();");
                break;
        }
    }

    /// <summary>
    /// Emits min-elements / max-elements bounds checks for a list or leaf-list node.
    /// </summary>
    private static void EmitMinMaxElementsCheck(StringBuilder body, IStatement schema, string targetName)
    {
        var schemaPath = EscapeForString(schema.XPath);

        if (schema.TryGetChild<MinElements>(out var minEl) && minEl!.Value > 0)
        {
            body.AppendLine($$"""
                              // min-elements {{minEl.Value}}
                              if ({{targetName}} is null || ((global::System.Collections.ICollection){{targetName}}).Count < {{minEl.Value}})
                              {
                                  throw new global::YangSupport.YangValidationException(
                                      "min-elements constraint violated: '{{targetName}}' requires at least {{minEl.Value}} entries",
                                      schemaPath: "{{schemaPath}}");
                              }
                              """);
        }

        if (schema.TryGetChild<MaxElements>(out var maxEl) && maxEl!.Value > 0 && !maxEl.IsUnbounded)
        {
            body.AppendLine($$"""
                              // max-elements {{maxEl.Value}}
                              if ({{targetName}} is not null && ((global::System.Collections.ICollection){{targetName}}).Count > {{maxEl.Value}})
                              {
                                  throw new global::YangSupport.YangValidationException(
                                      "max-elements constraint violated: '{{targetName}}' allows at most {{maxEl.Value}} entries",
                                      schemaPath: "{{schemaPath}}");
                              }
                              """);
        }
    }

    /// <summary>
    /// Emits unique constraint validation for a list node.
    /// Per RFC 7950 §7.8.3, no two list entries may have the same combination of values
    /// for the leaves specified in a unique statement.
    /// </summary>
    private static void EmitUniqueConstraintCheck(StringBuilder body, List list)
    {
        var uniqueStatements = list.Children.OfType<Unique>().ToArray();
        if (uniqueStatements.Length == 0) return;

        var schemaPath = EscapeForString(list.XPath);

        foreach (var unique in uniqueStatements)
        {
            var propNames = unique.PropertyNames;
            if (propNames.Length == 0) continue;

            // Build a tuple expression for the unique key
            var tupleExpr = propNames.Length == 1
                ? $"__e.{propNames[0]}"
                : $"({string.Join(", ", propNames.Select(p => $"__e.{p}"))})";

            var uniqueFieldsStr = string.Join(" ", unique.Identifiers);
            body.AppendLine($$"""
                              // unique: {{uniqueFieldsStr}}
                              if ({{list.TargetName}} is not null)
                              {
                                  var __seen = new global::System.Collections.Generic.HashSet<object?>();
                                  foreach (var __e in {{list.TargetName}})
                                  {
                                      var __uniqueKey = (object?){{tupleExpr}};
                                      if (!__seen.Add(__uniqueKey))
                                      {
                                          throw new global::YangSupport.YangValidationException(
                                              "unique constraint violated: duplicate values for '{{EscapeForString(uniqueFieldsStr)}}' in list '{{list.TargetName}}'",
                                              schemaPath: "{{schemaPath}}");
                                      }
                                  }
                              }
                              """);
        }
    }

    /// <summary>
    /// Emits a require-instance validation check for a leaf typed as instance-identifier.
    /// When require-instance is true (the default per RFC 7950 §9.13), the referenced
    /// node must exist in the data tree at validation time.
    /// </summary>
    private static void EmitInstanceIdentifierCheck(StringBuilder body, Leaf leaf)
    {
        var typeChild = leaf.Children.OfType<Type>().FirstOrDefault();
        if (typeChild is null) return;

        // Only emit for direct instance-identifier types, not typedefs that wrap it.
        // Typedef wrappers generate custom classes without a Resolve() method.
        if (typeChild.Argument != "instance-identifier") return;

        // Check require-instance (defaults to true per RFC 7950 §9.13)
        var requireInstance = true;
        if (typeChild.TryGetChild<RequireInstance>(out var ri))
        {
            requireInstance = ri!.Value;
        }

        if (!requireInstance) return;

        var schemaPath = EscapeForString(leaf.XPath);
        body.AppendLine($$"""
                          // require-instance true for instance-identifier leaf '{{leaf.TargetName}}'
                          if ({{leaf.TargetName}} is not null && this.YangParent is not null)
                          {
                              // Walk up to Configuration root via YangParent chain
                              object? __root = this;
                              while (true)
                              {
                                  var __parentProp = __root!.GetType().GetProperty("YangParent");
                                  if (__parentProp is null) break;
                                  var __next = __parentProp.GetValue(__root);
                                  if (__next is null) break;
                                  __root = __next;
                              }
                              var __resolved = {{leaf.TargetName}}.Resolve(__root!);
                              if (__resolved is null)
                              {
                                  throw new global::YangSupport.YangValidationException(
                                      "require-instance constraint violated: instance-identifier '{{leaf.TargetName}}' references a non-existent node",
                                      schemaPath: "{{schemaPath}}");
                              }
                          }
                          """);
    }

    private static void EmitConstraint(StringBuilder body, XPathTranslator translator,
        IStatement schema, string xpathSource, string kind,
        string? appTag, string? message)
    {
        string compiled;
        try
        {
            var ast = XPathParser.Parse(xpathSource);
            compiled = translator.TranslateBoolean(ast);
        }
        catch (UntranslatableXPathException ex)
        {
            Log.Write($"Untranslatable {kind} XPath at {schema.XPath}: {ex.Message}");
            body.AppendLine($"// {kind}: {OneLine(xpathSource)}");
            body.AppendLine($"// (XPath above cannot be translated by this version; constraint not enforced.)");
            return;
        }
        catch (XPathParseException ex)
        {
            Log.Write($"Failed to parse {kind} XPath at {schema.XPath}: {ex.Message}");
            body.AppendLine($"// {kind}: {OneLine(xpathSource)}");
            body.AppendLine($"// (XPath above failed to parse; constraint not enforced.)");
            return;
        }

        var schemaPath = EscapeForString(schema.XPath);
        var msgArg = message is null
            ? $"\"YANG '{kind}' constraint violated at {schemaPath}\""
            : $"\"{EscapeForString(message)}\"";
        var appTagArg = appTag is null ? "null" : $"\"{EscapeForString(appTag)}\"";

        body.AppendLine($"// {kind}: {OneLine(xpathSource)}");
        body.AppendLine($$"""
                          if (!({{compiled}}))
                          {
                              throw new global::YangSupport.YangValidationException(
                                  {{msgArg}},
                                  schemaPath: "{{schemaPath}}",
                                  errorAppTag: {{appTagArg}});
                          }
                          """);
    }

    private static string OneLine(string s)
    {
        return s.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string EscapeForString(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
