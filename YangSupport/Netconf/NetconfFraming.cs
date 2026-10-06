using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YangSupport.Netconf;

/// <summary>
/// NETCONF message framing per RFC 6242.
/// Supports both base:1.0 (End-of-Message) and base:1.1 (Chunked) framing.
/// </summary>
public static class NetconfFraming
{
    private const string EndOfMessage = "]]>]]>";
    private const string ChunkHeader = "\n#";
    private const string ChunkEnd = "\n##\n";

    /// <summary>
    /// Encode a message using End-of-Message framing (base:1.0).
    /// </summary>
    public static byte[] EncodeEom(string message)
    {
        return Encoding.UTF8.GetBytes(message + EndOfMessage);
    }

    /// <summary>
    /// Encode a message using Chunked framing (base:1.1).
    /// </summary>
    public static byte[] EncodeChunked(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        var header = $"\n#{data.Length}\n";
        var headerBytes = Encoding.UTF8.GetBytes(header);
        var endBytes = Encoding.UTF8.GetBytes(ChunkEnd);

        var result = new byte[headerBytes.Length + data.Length + endBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
        Buffer.BlockCopy(data, 0, result, headerBytes.Length, data.Length);
        Buffer.BlockCopy(endBytes, 0, result, headerBytes.Length + data.Length, endBytes.Length);
        return result;
    }

    /// <summary>
    /// Encode a message using Chunked framing (base:1.1) when <paramref name="base11"/> is set,
    /// otherwise End-of-Message framing (base:1.0).
    /// </summary>
    public static byte[] Encode(string message, bool base11)
    {
        return base11 ? EncodeChunked(message) : EncodeEom(message);
    }

    /// <summary>
    /// Frame <paramref name="message"/> (see <see cref="Encode"/>), write it to the stream and flush.
    /// </summary>
    public static async Task WriteMessageAsync(Stream stream, string message, bool base11)
    {
        var encoded = Encode(message, base11);
        await stream.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Read a complete NETCONF message using Chunked framing (base:1.1) when
    /// <paramref name="base11"/> is set, otherwise End-of-Message framing (base:1.0).
    /// </summary>
    public static Task<string> ReadMessageAsync(Stream stream, bool base11, CancellationToken ct = default)
    {
        return base11 ? ReadChunkedMessageAsync(stream, ct) : ReadEomMessageAsync(stream, ct);
    }

    /// <summary>
    /// Read a complete NETCONF message from a stream using EOM framing.
    /// </summary>
    public static async Task<string> ReadEomMessageAsync(Stream stream, CancellationToken ct = default)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();
        var timeout = TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                continue;
            }

            sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            var content = sb.ToString();
            var delimiterIndex = content.IndexOf(EndOfMessage, StringComparison.Ordinal);
            if (delimiterIndex >= 0)
            {
                return content.Substring(0, delimiterIndex);
            }
        }

        throw new TimeoutException($"Timed out waiting for NETCONF EOM. Received: {sb}");
    }

    /// <summary>
    /// Read a complete NETCONF message from a stream using Chunked framing (base:1.1).
    /// </summary>
    public static async Task<string> ReadChunkedMessageAsync(Stream stream, CancellationToken ct = default)
    {
        var result = new StringBuilder();
        var lineBuffer = new StringBuilder();
        var timeout = TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + timeout;

        // States: ReadingHeader, ReadingData, Done
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            // Read chunk header: \n#<size>\n or \n##\n
            var headerLine = await ReadLineAsync(stream, ct).ConfigureAwait(false);

            if (headerLine == null)
                continue;

            // Strip leading \n if present
            headerLine = headerLine.TrimStart('\n');

            if (!headerLine.StartsWith("#"))
                continue;

            var sizeStr = headerLine.Substring(1);

            // End of chunks marker
            if (sizeStr == "#")
                return result.ToString();

            if (!int.TryParse(sizeStr, out var chunkSize) || chunkSize <= 0)
                throw new InvalidOperationException($"Invalid chunk size: '{sizeStr}'");

            // Read exactly chunkSize bytes
            var chunkData = new byte[chunkSize];
            var totalRead = 0;
            while (totalRead < chunkSize)
            {
                var read = await stream.ReadAsync(chunkData, totalRead, chunkSize - totalRead).ConfigureAwait(false);
                if (read == 0) throw new IOException("Stream closed while reading chunk data");
                totalRead += read;
            }

            result.Append(Encoding.UTF8.GetString(chunkData));
        }

        throw new TimeoutException($"Timed out reading chunked NETCONF message. Got: {result}");
    }

    private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(buf, 0, 1).ConfigureAwait(false);
            if (read == 0)
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                continue;
            }

            var c = (char)buf[0];
            sb.Append(c);

            // A header line ends with \n after the size: \n#<size>\n
            if (c == '\n' && sb.Length > 2)
                return sb.ToString().TrimEnd('\n');
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
