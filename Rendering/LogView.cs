using System.Text;
using System.Windows.Forms;
using ComServer.Models;

namespace ComServer.Rendering;

/// <summary>
/// <see cref="RichTextBox"/> 的高性能日志视图。
/// <para>
/// 设计要点：
/// <list type="bullet">
/// <item>结构化保存全部日志条目，切换“文本 / HEX / 时间戳”时就地重排，不需要重新采集数据。</item>
/// <item>追加时只对本次新增的片段着色，不触碰已有内容。</item>
/// <item>超过行数上限时批量裁剪最旧的条目，把单次开销摊薄。</item>
/// </list>
/// </para>
/// </summary>
public sealed class LogView
{
    /// <summary>单次着色的字符上限；超过则保持默认颜色，避免 UI 线程长时间阻塞。</summary>
    private const int HighlightBudgetChars = 256 * 1024;

    private readonly RichTextBox _box;
    private readonly List<LogEntry> _entries = [];
    private readonly List<int> _lineCounts = [];
    private readonly int _maxEntries;
    private readonly int _trimBatch;

    public LogView(RichTextBox box, int maxEntries = 5000)
    {
        _box = box ?? throw new ArgumentNullException(nameof(box));
        _maxEntries = Math.Max(1, maxEntries);

        // 一次性多裁一些，避免每追加一条都触发一次删除。
        _trimBatch = Math.Max(1, _maxEntries / 10);
    }

    /// <summary>是否自动滚动到底部。若用户已经向上滚动，则暂时不打扰他。</summary>
    public bool AutoScroll { get; set; } = true;

    public LogFormatOptions Options { get; } = new();

    public int Count => _entries.Count;

    /// <summary>追加一条日志。</summary>
    public void Add(LogEntry entry)
    {
        _entries.Add(entry);
        _lineCounts.Add(Append(entry));

        if (_entries.Count > _maxEntries)
        {
            Trim();
        }
    }

    public void Clear()
    {
        _entries.Clear();
        _lineCounts.Clear();
        _box.Clear();
    }

    /// <summary>显示格式（HEX / 时间戳 / 编码）变化后，用新格式重排已有日志。</summary>
    public void RefreshFormatting()
    {
        var texts = new string[_entries.Count];
        var builder = new StringBuilder();

        for (var i = 0; i < _entries.Count; i++)
        {
            var text = LogEntryFormatter.Format(_entries[i], Options);
            texts[i] = text;
            _lineCounts[i] = CountLines(text);
            builder.Append(text);
        }

        var readOnly = _box.ReadOnly;
        _box.ReadOnly = false;
        _box.Text = builder.ToString();

        // 重新着色；超出预算的部分保持默认颜色。
        var offset = 0;
        var colored = 0;
        for (var i = 0; i < texts.Length; i++)
        {
            var length = texts[i].Length;
            if (colored + length > HighlightBudgetChars)
            {
                break;
            }

            _box.Select(offset, length);
            _box.SelectionColor = ColorFor(_entries[i].Direction);
            offset += length;
            colored += length;
        }

        _box.Select(_box.TextLength, 0);
        _box.SelectionColor = _box.ForeColor;
        _box.ReadOnly = readOnly;

        if (AutoScroll)
        {
            ScrollToEnd();
        }
    }

    /// <summary>把当前显示内容保存为文本文件（与界面看到的一致）。</summary>
    public void Save(string path) => File.WriteAllText(path, _box.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    /// <summary>追加单条并返回它占用的行数。</summary>
    private int Append(LogEntry entry)
    {
        var text = LogEntryFormatter.Format(entry, Options);
        var atBottom = AutoScroll && IsScrolledToBottom();
        var start = _box.TextLength;

        _box.AppendText(text);

        var length = _box.TextLength - start;
        if (length <= HighlightBudgetChars)
        {
            _box.Select(start, length);
            _box.SelectionColor = ColorFor(entry.Direction);
        }

        _box.Select(_box.TextLength, 0);
        _box.SelectionColor = _box.ForeColor;

        if (atBottom)
        {
            ScrollToEnd();
        }

        return CountLines(text);
    }

    /// <summary>从最旧的条目开始裁剪，直到回到上限以内。</summary>
    private void Trim()
    {
        var removeCount = Math.Min(_trimBatch, _entries.Count);
        var linesToRemove = 0;

        for (var i = 0; i < removeCount; i++)
        {
            linesToRemove += _lineCounts[i];
        }

        _entries.RemoveRange(0, removeCount);
        _lineCounts.RemoveRange(0, removeCount);

        var firstKeptChar = _box.GetFirstCharIndexFromLine(linesToRemove);
        if (firstKeptChar <= 0)
        {
            return;
        }

        var readOnly = _box.ReadOnly;
        _box.ReadOnly = false;
        _box.Select(0, firstKeptChar);
        _box.SelectedText = string.Empty;
        _box.Select(0, 0);
        _box.ReadOnly = readOnly;
    }

    private void ScrollToEnd()
    {
        _box.SelectionStart = _box.TextLength;
        _box.SelectionLength = 0;
        _box.ScrollToCaret();
    }

    private bool IsScrolledToBottom()
    {
        if (_box.TextLength == 0)
        {
            return true;
        }

        var probe = new Point(1, Math.Max(1, _box.ClientSize.Height - 2));
        var lastVisible = _box.GetCharIndexFromPosition(probe);
        return lastVisible >= _box.TextLength - 1;
    }

    private static int CountLines(string text)
    {
        var lines = 0;
        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                lines++;
            }
        }

        return Math.Max(1, lines);
    }

    private Color ColorFor(LogDirection direction) => direction switch
    {
        LogDirection.Rx => Color.FromArgb(0, 120, 60),    // 串口 → TCP：绿色
        LogDirection.Tx => Color.FromArgb(0, 82, 204),    // TCP → 串口：蓝色
        LogDirection.Error => Color.FromArgb(196, 43, 28), // 错误：红色
        _ => Color.FromArgb(110, 110, 110),               // 系统：灰色
    };
}
