using System.Diagnostics;
using System.Text;

namespace YangParser.Generator;

public static class Log
{
    private static StringBuilder m_builder = new();
    public static void Clear() => m_builder.Clear();
    public static void Write(string message) => m_builder.AppendLine(message);
    public static string Get() => m_builder.ToString();
}