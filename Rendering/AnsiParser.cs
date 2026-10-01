using System.Drawing;
using System.Text;

namespace ComServer.Rendering;

/// <summary>
/// 精简的 ANSI 转义序列解析器，用于把设备输出渲染成“类终端”的带样式文本。
/// <para>
/// 处理范围：
/// <list type="bullet">
/// <item>SGR（<c>ESC […m</c>）：前景 / 背景色、粗体、下划线、反显，支持 256 色与真彩。</item>
/// <item>控制符：<c>\r</c> 视为换行、<c>\b</c> 退格删一个字符、<c>\t</c> 原样保留。</item>
/// <item>其余 CSI / OSC / 两字符转义序列整段跳过（不显示成乱码）。</item>
/// <item>其他不可见控制符按终端的做法直接忽略，且不会替换成 <c>.</c>。</item>
/// </list>
/// </para>
/// 没有转义序列时，等价于“原样显示 + 换行归一化”，因此可以无条件调用。
/// </summary>
public static class AnsiParser
{
    private const char Escape = '\u001B';
    private const string NewLine = "\r\n";

    public static List<StyledRun> Parse(string text, Color defaultForeground, Color defaultBackground)
    {
        var runs = new List<StyledRun>(4);
        var buffer = new StringBuilder(text.Length);

        Color? foreground = null;
        Color? background = null;
        var bold = false;
        var underline = false;
        var reverse = false;

        void Flush()
        {
            if (buffer.Length == 0)
            {
                return;
            }

            var resolved = Resolve(foreground, background, reverse, defaultForeground, defaultBackground);
            runs.Add(new StyledRun(buffer.ToString(), resolved.Foreground, resolved.Background, bold, underline));
            buffer.Clear();
        }

        void ApplySgr(string parameters)
        {
            // 样式切换必须断开当前片段，否则已输出的文字会被新样式“污染”。
            Flush();

            var parts = parameters.Length == 0 ? ["0"] : parameters.Split(';');

            for (var i = 0; i < parts.Length; i++)
            {
                var code = parts[i].Length == 0 ? 0 : (int.TryParse(parts[i], out var value) ? value : 0);

                switch (code)
                {
                    case 0:
                        foreground = null;
                        background = null;
                        bold = false;
                        underline = false;
                        reverse = false;
                        break;
                    case 1: bold = true; break;
                    case 22: bold = false; break;
                    case 4: underline = true; break;
                    case 24: underline = false; break;
                    case 7: reverse = true; break;
                    case 27: reverse = false; break;
                    case 39: foreground = null; break;
                    case 49: background = null; break;

                    case >= 30 and <= 37:
                        foreground = TerminalPalette.Index(code - 30);
                        break;
                    case >= 40 and <= 47:
                        background = TerminalPalette.Index(code - 40);
                        break;
                    case >= 90 and <= 97:
                        foreground = TerminalPalette.Index(code - 90 + 8);
                        break;
                    case >= 100 and <= 107:
                        background = TerminalPalette.Index(code - 100 + 8);
                        break;

                    case 38 or 48:
                        var isForeground = code == 38;
                        if (i + 2 < parts.Length && parts[i + 1] == "5" && int.TryParse(parts[i + 2], out var index))
                        {
                            var color = TerminalPalette.Index(index);
                            if (isForeground) foreground = color; else background = color;
                            i += 2;
                        }
                        else if (i + 4 < parts.Length && parts[i + 1] == "2"
                                 && int.TryParse(parts[i + 2], out var r)
                                 && int.TryParse(parts[i + 3], out var g)
                                 && int.TryParse(parts[i + 4], out var b))
                        {
                            var color = Color.FromArgb(Clamp(r), Clamp(g), Clamp(b));
                            if (isForeground) foreground = color; else background = color;
                            i += 4;
                        }

                        break;
                }
            }
        }

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch == Escape)
            {
                i = ConsumeEscape(text, i, ApplySgr);
                continue;
            }

            switch (ch)
            {
                case '\r':
                    // 终端的 \r 是回到行首覆盖；日志是追加式的，退化成换行更实用。
                    // 注意要跳过后面的 \n，否则 CRLF 会被算成两次换行。
                    Flush();
                    buffer.Append(NewLine);
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    break;

                case '\n':
                    buffer.Append(NewLine);
                    break;

                case '\t':
                    buffer.Append('\t');
                    break;

                case '\b':
                    // 退格：删掉当前片段里最后一个字符。
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                    }

                    break;

                default:
                    if (!char.IsControl(ch))
                    {
                        buffer.Append(ch);
                    }

                    break;
            }
        }

        Flush();
        return runs;
    }

    /// <summary>跳过起始位置处的转义序列，返回该序列最后一个字符的下标。</summary>
    private static int ConsumeEscape(string text, int start, Action<string> applySgr)
    {
        var i = start + 1;
        if (i >= text.Length)
        {
            return start;
        }

        switch (text[i])
        {
            case '[':
            {
                // CSI：参数字节 0x30-0x3F，中间字节 0x20-0x2F，终止字节 0x40-0x7E。
                var j = i + 1;
                var parameters = new StringBuilder();

                while (j < text.Length && text[j] is >= '\u0020' and <= '\u003F')
                {
                    parameters.Append(text[j]);
                    j++;
                }

                if (j >= text.Length)
                {
                    return text.Length - 1;
                }

                if (text[j] == 'm')
                {
                    applySgr(parameters.ToString());
                }

                return j;
            }

            case ']':
            {
                // OSC：以 BEL 或 ST(ESC \) 结束。
                var j = i + 1;
                while (j < text.Length)
                {
                    if (text[j] == '\a')
                    {
                        break;
                    }

                    if (text[j] == Escape && j + 1 < text.Length && text[j + 1] == '\\')
                    {
                        j++;
                        break;
                    }

                    j++;
                }

                return Math.Min(j, text.Length - 1);
            }

            default:
                // 两字符转义序列（如 ESC ( B）。
                return i;
        }
    }

    private static StyledRun Resolve(Color? foreground, Color? background, bool reverse, Color defaultForeground, Color defaultBackground)
    {
        if (!reverse)
        {
            return new StyledRun(string.Empty, foreground, background, false, false);
        }

        // 反显：前景背景互换。缺省的一侧用日志默认色补齐，
        // 这样白底上的反显文字会变成“白字 + 有底色”，仍然可读。
        return new StyledRun(string.Empty, background ?? defaultBackground, foreground ?? defaultForeground, false, false);
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 255);
}
