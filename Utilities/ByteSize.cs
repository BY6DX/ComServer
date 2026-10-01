using System.Globalization;

namespace ComServer.Utilities;

/// <summary>把字节数格式化成便于阅读的字符串。</summary>
public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value:0.##} {Units[unit]}");
    }
}
