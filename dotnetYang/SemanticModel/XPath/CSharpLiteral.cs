using System.Text;

namespace YangParser.SemanticModel.XPath;

/// <summary>Helpers for embedding arbitrary text inside generated C# string literals.</summary>
internal static class CSharpLiteral
{
    /// <summary>Escapes <paramref name="s"/> for use inside a regular (non-verbatim) C# string literal.</summary>
    public static string Escape(string s)
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
