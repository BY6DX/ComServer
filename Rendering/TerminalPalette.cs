using System.Drawing;

namespace ComServer.Rendering;

/// <summary>一段带样式的文本。颜色为 <c>null</c> 表示沿用默认颜色。</summary>
public readonly record struct StyledRun(string Text, Color? Foreground, Color? Background, bool Bold, bool Underline);

/// <summary>
/// 终端调色板。
/// 基础 16 色取 VS Code 浅色主题的终端配色——日志框是白底，
/// conhost 的亮黄 / 亮白在上面根本看不见，所以不能用默认的那一套。
/// </summary>
public static class TerminalPalette
{
    private static readonly Color[] Base16 =
    [
        Color.FromArgb(0x00, 0x00, 0x00), // 0  黑
        Color.FromArgb(0xCD, 0x31, 0x31), // 1  红
        Color.FromArgb(0x00, 0xBC, 0x00), // 2  绿
        Color.FromArgb(0x94, 0x98, 0x00), // 3  黄
        Color.FromArgb(0x04, 0x51, 0xA5), // 4  蓝
        Color.FromArgb(0xBC, 0x05, 0xBC), // 5  洋红
        Color.FromArgb(0x05, 0x98, 0xBC), // 6  青
        Color.FromArgb(0x55, 0x55, 0x55), // 7  白
        Color.FromArgb(0x66, 0x66, 0x66), // 8  亮黑
        Color.FromArgb(0xCD, 0x31, 0x31), // 9  亮红
        Color.FromArgb(0x14, 0xCE, 0x14), // 10 亮绿
        Color.FromArgb(0xB5, 0xBA, 0x00), // 11 亮黄
        Color.FromArgb(0x04, 0x51, 0xA5), // 12 亮蓝
        Color.FromArgb(0xBC, 0x05, 0xBC), // 13 亮洋红
        Color.FromArgb(0x05, 0x98, 0xBC), // 14 亮青
        Color.FromArgb(0xA5, 0xA5, 0xA5), // 15 亮白
    ];

    /// <summary>取 256 色索引对应的颜色：0-15 基础色，16-231 为 6x6x6 色立方，232-255 为灰阶。</summary>
    public static Color Index(int index)
    {
        if (index < 0)
        {
            return Base16[7];
        }

        if (index < 16)
        {
            return Base16[index];
        }

        if (index < 232)
        {
            var value = index - 16;
            return Color.FromArgb(Level(value / 36 % 6), Level(value / 6 % 6), Level(value % 6));
        }

        if (index < 256)
        {
            var gray = (byte)(8 + (index - 232) * 10);
            return Color.FromArgb(gray, gray, gray);
        }

        return Base16[7];
    }

    private static int Level(int step) => step == 0 ? 0 : 55 + step * 40;
}
