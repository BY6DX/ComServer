using System.Globalization;
using System.Text;
using ComServer.Models;

namespace ComServer.Rendering;

/// <summary>日志显示选项，可在运行时切换。</summary>
public sealed class LogFormatOptions
{
    public bool ShowTimestamp { get; set; } = true;

    public PayloadDisplayMode Mode { get; set; } = PayloadDisplayMode.Text;

    public Encoding TextEncoding { get; set; } = Encoding.UTF8;

    public string TimestampFormat { get; set; } = "HH:mm:ss.fff";
}

/// <summary>把 <see cref="LogEntry"/> 渲染成一行（HEX 超长时多行）带前缀的文本。</summary>
public static class LogEntryFormatter
{
    private const string NewLine = "\r\n";

    public static string Format(LogEntry entry, LogFormatOptions options)
    {
        var prefix = BuildPrefix(entry, options);
        var body = entry.IsTextOnly
            ? Encoding.UTF8.GetString(entry.Payload)
            : BuildBody(entry.Payload, prefix.Length, options);

        return string.Concat(prefix, body, NewLine);
    }

    private static string BuildPrefix(LogEntry entry, LogFormatOptions options)
    {
        var builder = new StringBuilder(48);

        if (options.ShowTimestamp)
        {
            builder.Append('[')
                   .Append(entry.Timestamp.ToString(options.TimestampFormat, CultureInfo.InvariantCulture))
                   .Append("] ");
        }

        builder.Append(entry.Direction switch
        {
            LogDirection.Rx => "RX",
            LogDirection.Tx => "TX",
            LogDirection.Error => "!!",
            _ => "--",
        }).Append(' ');

        if (!string.IsNullOrEmpty(entry.Source))
        {
            builder.Append('(').Append(entry.Source).Append(") ");
        }

        return builder.ToString();
    }

    private static string BuildBody(byte[] payload, int indentWidth, LogFormatOptions options)
    {
        if (payload.Length == 0)
        {
            return options.Mode == PayloadDisplayMode.Hex ? "<空>" : string.Empty;
        }

        if (options.Mode != PayloadDisplayMode.Hex || payload.Length <= PayloadFormatter.HexBytesPerLine)
        {
            return PayloadFormatter.Format(payload, options.Mode, options.TextEncoding);
        }

        // 长数据按 16 字节折行，续行用与前缀等宽的空格缩进，视觉上仍是一个整体。
        var builder = new StringBuilder(payload.Length * 3 + payload.Length / PayloadFormatter.HexBytesPerLine * 8);
        var indent = new string(' ', indentWidth);

        for (var offset = 0; offset < payload.Length; offset += PayloadFormatter.HexBytesPerLine)
        {
            if (offset > 0)
            {
                builder.Append(NewLine).Append(indent);
            }

            var count = Math.Min(PayloadFormatter.HexBytesPerLine, payload.Length - offset);
            builder.Append(PayloadFormatter.ToHex(payload.AsSpan(offset, count)));
        }

        return builder.ToString();
    }
}
