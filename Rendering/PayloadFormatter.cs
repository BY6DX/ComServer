using System.Text;

namespace ComServer.Rendering;

/// <summary>日志数据的显示格式。</summary>
public enum PayloadDisplayMode
{
    /// <summary>按文本显示。</summary>
    Text,

    /// <summary>按十六进制字节显示。</summary>
    Hex,
}

/// <summary>把原始字节转换成可直接放进日志框的字符串。</summary>
public static class PayloadFormatter
{
    private const string HexDigits = "0123456789ABCDEF";

    public static string Format(ReadOnlySpan<byte> data, PayloadDisplayMode mode, Encoding? encoding = null)
        => mode == PayloadDisplayMode.Hex ? ToHex(data) : ToText(data, encoding);

    /// <summary>转成大写十六进制，字节之间用空格分隔，例如 <c>01 A2 FF</c>。</summary>
    public static string ToHex(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        // 每个字节固定占 3 个字符（两位 + 一个分隔符），末位不写分隔符。
        var buffer = new char[data.Length * 3 - 1];
        var position = 0;

        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0)
            {
                buffer[position++] = ' ';
            }

            var value = data[i];
            buffer[position++] = HexDigits[value >> 4];
            buffer[position++] = HexDigits[value & 0x0F];
        }

        return new string(buffer);
    }

    /// <summary>
    /// 按指定编码解码为文本。
    /// 这里不做任何换行归一化或控制符过滤：渲染前的处理统一交给
    /// <see cref="AnsiParser"/>，避免同一份文本被处理两次。
    /// </summary>
    public static string ToText(ReadOnlySpan<byte> data, Encoding? encoding = null)
        => data.IsEmpty ? string.Empty : (encoding ?? Encoding.UTF8).GetString(data);

    /// <summary>
    /// 解析用户输入的 HEX 字符串。允许空格、逗号、冒号、短横线、换行以及 <c>0x</c> 前缀。
    /// </summary>
    public static bool TryParseHex(string? text, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var cleaned = text.Replace("0x", " ", StringComparison.OrdinalIgnoreCase);
        var result = new List<byte>(cleaned.Length / 2 + 1);
        var highNibble = -1;

        foreach (var ch in cleaned)
        {
            if (ch is ' ' or ',' or ':' or '-' or '\r' or '\n' or '\t')
            {
                // 遇到分隔符时，把落单的高四位补成 0xN0，避免直接丢弃用户输入。
                if (highNibble >= 0)
                {
                    result.Add((byte)(highNibble << 4));
                    highNibble = -1;
                }

                continue;
            }

            var value = HexValue(ch);
            if (value < 0)
            {
                return false;
            }

            if (highNibble < 0)
            {
                highNibble = value;
            }
            else
            {
                result.Add((byte)((highNibble << 4) | value));
                highNibble = -1;
            }
        }

        if (highNibble >= 0)
        {
            result.Add((byte)(highNibble << 4));
        }

        bytes = [.. result];
        return true;
    }

    private static int HexValue(char ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'a' and <= 'f' => ch - 'a' + 10,
        >= 'A' and <= 'F' => ch - 'A' + 10,
        _ => -1,
    };
}
