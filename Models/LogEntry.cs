using System.Text;

namespace ComServer.Models;

/// <summary>日志行方向。RX = 串口 → TCP，TX = TCP → 串口。</summary>
public enum LogDirection
{
    /// <summary>串口收到的数据，已转发给 TCP 客户端。</summary>
    Rx,

    /// <summary>TCP 客户端发来的数据，已转发给串口。</summary>
    Tx,

    /// <summary>系统提示，例如客户端连接、串口打开。</summary>
    System,

    /// <summary>错误提示。</summary>
    Error,
}

/// <summary>
/// 一条转发记录。
/// 这里刻意保存 <b>原始字节</b>而不是格式化后的字符串：切换“文本 / HEX / 编码”时可以
/// 立即用新格式重排历史日志，而不必重新抓取数据。
/// </summary>
/// <param name="Timestamp">记录时间（本地时间）。</param>
/// <param name="Direction">转发方向。</param>
/// <param name="Payload">原始数据；系统类消息存放的是 UTF-8 文本。</param>
/// <param name="Source">来源标记，例如 "#1"（客户端）或 "手动"。</param>
public readonly record struct LogEntry(
    DateTime Timestamp,
    LogDirection Direction,
    byte[] Payload,
    string? Source = null)
{
    public static LogEntry System(DateTime timestamp, string message)
        => new(timestamp, LogDirection.System, Encoding.UTF8.GetBytes(message));

    public static LogEntry Error(DateTime timestamp, string message)
        => new(timestamp, LogDirection.Error, Encoding.UTF8.GetBytes(message));

    /// <summary>系统 / 错误消息始终按文本显示，不参与 HEX 格式化。</summary>
    public bool IsTextOnly => Direction is LogDirection.System or LogDirection.Error;
}
