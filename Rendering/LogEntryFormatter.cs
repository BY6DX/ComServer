using System.Drawing;
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

    /// <summary>终端渲染时的默认前景色（未指定颜色的文字用它）。</summary>
    public Color DefaultForeground { get; set; } = LogColors.Foreground;

    /// <summary>终端渲染时的默认背景色（反显和背景色的回退值）。</summary>
    public Color DefaultBackground { get; set; } = LogColors.Background;
}

/// <summary>把 <see cref="LogEntry"/> 渲染成带前缀的文本或带样式的片段。</summary>
public static class LogEntryFormatter
{
    private const string NewLine = "\r\n";

    /// <summary>
    /// 渲染程序自己生成的系统 / 错误消息：纯文本，不做终端解析。
    /// （这类消息的正文来自代码里的固定字符串，不含设备数据。）
    /// </summary>
    public static string FormatMessage(LogEntry entry, LogFormatOptions options)
        => string.Concat(BuildPrefix(entry, options), Encoding.UTF8.GetString(entry.Payload), NewLine);

    /// <summary>
    /// 生成带样式的片段：前缀按方向着色，正文自动识别 ANSI 转义序列并渲染颜色。
    /// 把设备输出（例如 ls --color）里的颜色如实体现在日志里；
    /// 不含转义序列的纯文本会原样输出，因此文本模式无需额外的模式切换。
    /// </summary>
    public static List<StyledRun> FormatRuns(LogEntry entry, LogFormatOptions options, Color prefixColor)
    {
        var prefix = BuildPrefix(entry, options);
        var body = PayloadFormatter.Format(entry.Payload, options.Mode, options.TextEncoding);
        var terminator = body.EndsWith('\n') ? string.Empty : NewLine;

        var runs = new List<StyledRun>(8)
        {
            new(prefix, prefixColor, null, false, false),
        };

        runs.AddRange(AnsiParser.Parse(body, options.DefaultForeground, options.DefaultBackground));

        if (terminator.Length > 0)
        {
            runs.Add(new StyledRun(terminator, null, null, false, false));
        }

        return runs;
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
}
