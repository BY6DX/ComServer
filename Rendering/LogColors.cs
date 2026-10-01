using System.Drawing;

namespace ComServer.Rendering;

/// <summary>日志配色。集中在一处，便于统一调整。</summary>
public static class LogColors
{
    /// <summary>日志框背景色，也是终端反显 / 默认背景色的回退值。</summary>
    public static readonly Color Background = Color.White;

    /// <summary>日志框默认前景色。</summary>
    public static readonly Color Foreground = Color.FromArgb(30, 30, 30);

    /// <summary>串口 → TCP。</summary>
    public static readonly Color Rx = Color.FromArgb(0, 120, 60);

    /// <summary>TCP → 串口。</summary>
    public static readonly Color Tx = Color.FromArgb(0, 82, 204);

    /// <summary>错误。</summary>
    public static readonly Color Error = Color.FromArgb(196, 43, 28);

    /// <summary>系统提示。</summary>
    public static readonly Color System = Color.FromArgb(110, 110, 110);

    public static Color ForDirection(Models.LogDirection direction) => direction switch
    {
        Models.LogDirection.Rx => Rx,
        Models.LogDirection.Tx => Tx,
        Models.LogDirection.Error => Error,
        _ => System,
    };
}
