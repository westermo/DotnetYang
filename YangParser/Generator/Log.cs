using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace YangParser.Generator;

public static class Log
{
    private static DateTime Start;
    private static FileStream? m_stream;

    private static FileStream Stream
    {
        get
        {
            if (m_stream is null) Start = DateTime.Now;
            return m_stream ??= new FileStream(@"C:\tmp\YangGenerator\log", FileMode.Create, FileAccess.Write,
                FileShare.ReadWrite);
        }
    }

    public static void Clear()
    {
        Write("END OF LOG\n*/");
        // m_stream?.Dispose();
        // m_writer?.Dispose();
        // m_stream = null;
        // m_writer = null;
    }

    private static StringBuilder? m_writer;
    private static StringBuilder writer => m_writer ??= new StringBuilder("/*\n");

    public static void Write(string message)
    {
        writer.AppendLine($"{(DateTime.Now - Start).TotalSeconds:F2}: " + message);
    }

    public static string Content => writer.ToString();

// }
}