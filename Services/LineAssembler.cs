using System;

namespace ComServer.Services;

/// <summary>
/// 按行组装字节流。
/// <para>
/// 串口 / TCP 的数据到达边界是任意的，一条日志往往被拆成多个分片。
/// 若直接把每个分片当成一条记录，就会出现“半行一行”的错乱换行，
/// 因此这里先把分片拼回完整的行，再由日志层加前缀显示。
/// </para>
/// <para>注意：只影响显示，转发走的始终是原始字节。</para>
/// </summary>
public sealed class LineAssembler
{
    /// <summary>
    /// 长期没有换行符时强制断行的字节数。
    /// 取 2KB：真实的日志行几乎不会超过这个长度，
    /// 而二进制流也不会因为等待太久才显示。
    /// </summary>
    private const int MaxLineBytes = 2048;

    private readonly Lock _sync = new();
    private readonly List<byte> _pending = [];

    /// <summary>最近一次收到数据的时间，用于判断“半行”是否已经停留过久。</summary>
    private DateTime _lastActivity = DateTime.MinValue;

    /// <summary>喂入一个分片，返回其中已经完整的行（不含行结束符）。</summary>
    public List<byte[]> Feed(ReadOnlySpan<byte> data)
    {
        var lines = new List<byte[]>();

        lock (_sync)
        {
            _lastActivity = DateTime.Now;

            var start = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var value = data[i];
                if (value != (byte)'\n' && value != (byte)'\r')
                {
                    continue;
                }

                // 换行符之前的部分与已有残留拼接成一行。
                Append(data[start..i]);
                lines.Add(TakeLine());

                // CRLF 只算一次换行。
                if (value == (byte)'\r' && i + 1 < data.Length && data[i + 1] == (byte)'\n')
                {
                    i++;
                }

                start = i + 1;
            }

            Append(data[start..]);

            // 一直没有换行符（例如二进制数据）时按块强制断行。
            while (_pending.Count > MaxLineBytes)
            {
                lines.Add([.. _pending.Take(MaxLineBytes)]);
                _pending.RemoveRange(0, MaxLineBytes);
            }
        }

        return lines;
    }

    /// <summary>
    /// 取出停留过久仍未成行的数据（例如没有换行的提示符），没有则返回 <c>null</c>。
    /// 这样交互式输出不会一直卡在缓冲里看不到。
    /// </summary>
    public byte[]? TakeIdle(TimeSpan idle, DateTime now)
    {
        lock (_sync)
        {
            if (_pending.Count == 0 || now - _lastActivity < idle)
            {
                return null;
            }

            return TakeLine();
        }
    }

    /// <summary>丢弃尚未成行的数据（用于重新开始服务）。</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _pending.Clear();
            _lastActivity = DateTime.MinValue;
        }
    }

    /// <summary>强制取出尚未成行的数据，用于停止服务时收尾。</summary>
    public byte[]? TakeRemaining()
    {
        lock (_sync)
        {
            return _pending.Count == 0 ? null : TakeLine();
        }
    }

    private void Append(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        foreach (var value in data)
        {
            _pending.Add(value);
        }
    }

    private byte[] TakeLine()
    {
        var line = _pending.ToArray();
        _pending.Clear();
        return line;
    }
}
