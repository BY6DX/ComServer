using System.Text;
using System.Windows.Forms;
using ComServer.Models;

namespace ComServer.Rendering;

/// <summary>
/// <see cref="RichTextBox"/> 的日志视图，同时具备“类终端”渲染能力。
/// <para>
/// 设计要点：
/// <list type="bullet">
/// <item>结构化保存全部日志条目，切换显示格式时就地重排，不需要重新采集数据。</item>
/// <item>正文自动识别 ANSI 转义序列，RX / TX 前缀则用颜色区分。</item>
/// <item>追加时只处理本次新增的片段，不触碰已有内容。</item>
/// <item>超过条目上限时批量裁剪最旧的记录，把单次开销摊薄。</item>
/// </list>
/// </para>
/// </summary>
public sealed class LogView : IDisposable
{
    /// <summary>单次着色的字符上限；超过则保持默认颜色，避免 UI 线程长时间阻塞。</summary>
    private const int HighlightBudgetChars = 256 * 1024;

    private readonly RichTextBox _box;
    private readonly List<LogEntry> _entries = [];
    private readonly List<int> _charCounts = [];
    private readonly Dictionary<FontStyle, Font> _fonts = [];
    private readonly int _maxEntries;
    private readonly int _trimBatch;

    // 缓存当前选择区属性，避免对 RichTextBox 重复设置同样的值。
    private FontStyle _currentStyle = FontStyle.Regular;
    private Color _currentForeground;
    private Color _currentBackground;

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
        _charCounts.Add(Append(entry));

        if (_entries.Count > _maxEntries)
        {
            Trim();
        }
    }

    public void Clear()
    {
        _entries.Clear();
        _charCounts.Clear();
        _box.Clear();
        ResetStyleCache();
    }

    /// <summary>显示格式（HEX / 时间戳 / 编码）变化后，用新格式重排已有日志。</summary>
    public void RefreshFormatting()
    {
        var allRuns = new List<StyledRun>(_entries.Count * 4);

        for (var i = 0; i < _entries.Count; i++)
        {
            var (runs, length) = Format(_entries[i]);
            _charCounts[i] = length;
            allRuns.AddRange(runs);
        }

        var readOnly = _box.ReadOnly;
        _box.ReadOnly = false;
        _box.Clear();
        ResetStyleCache();

        var budget = HighlightBudgetChars;
        foreach (var run in allRuns)
        {
            Apply(run, ref budget);
        }

        ResetSelection();
        _box.ReadOnly = readOnly;

        if (AutoScroll)
        {
            ScrollToEnd();
        }
    }

    public void Dispose()
    {
        foreach (var font in _fonts.Values)
        {
            font.Dispose();
        }

        _fonts.Clear();
    }

    /// <summary>把当前显示内容保存为文本文件（与界面看到的一致）。</summary>
    public void Save(string path) => File.WriteAllText(path, _box.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    /// <summary>追加单条并返回它占用的字符数（用于后续裁剪定位）。</summary>
    private int Append(LogEntry entry)
    {
        var (runs, length) = Format(entry);
        var atBottom = AutoScroll && IsScrolledToBottom();

        var budget = HighlightBudgetChars;
        foreach (var run in runs)
        {
            Apply(run, ref budget);
        }

        ResetSelection();

        if (atBottom)
        {
            ScrollToEnd();
        }

        return length;
    }

    /// <summary>把一条记录渲染成带样式的片段，并返回可见字符总数。</summary>
    private (List<StyledRun> Runs, int Length) Format(LogEntry entry)
    {
        // 系统 / 错误消息是程序自己生成的，直接按纯文本渲染。
        if (entry.IsTextOnly)
        {
            var text = LogEntryFormatter.FormatMessage(entry, Options);
            return ([new StyledRun(text, LogColors.ForDirection(entry.Direction), null, false, false)], text.Length);
        }

        // 其他内容统一走终端渲染：不带转义序列的纯文本会原样输出，
        // 带 ANSI 颜色时还原颜色，因此不需要额外的模式切换。
        var runs = LogEntryFormatter.FormatRuns(entry, Options, LogColors.ForDirection(entry.Direction));

        var length = 0;
        foreach (var run in runs)
        {
            length += run.Text.Length;
        }

        return (runs, length);
    }

    /// <summary>写入一个片段：只有属性真正变化时才写回 RichTextBox，避免多余的消息往返。</summary>
    private void Apply(StyledRun run, ref int budget)
    {
        if (run.Text.Length == 0)
        {
            return;
        }

        SetFontStyle((run.Bold ? FontStyle.Bold : FontStyle.Regular) | (run.Underline ? FontStyle.Underline : 0));

        var hasBackground = run.Background is { } background && background != _box.BackColor;
        var foreground = run.Foreground ?? _box.ForeColor;
        var fill = hasBackground ? run.Background!.Value : _box.BackColor;

        // 只有变化时才写回：SelectionColor / SelectionBackColor 每次赋值都会向
        // RichTextBox 发消息，历史区重排时这一步开销最大。
        if (_currentForeground != foreground)
        {
            _box.SelectionColor = foreground;
            _currentForeground = foreground;
        }

        if (_currentBackground != fill)
        {
            _box.SelectionBackColor = fill;
            _currentBackground = fill;
        }

        if (budget > 0)
        {
            budget -= run.Text.Length;
            _box.AppendText(run.Text);
        }
        else
        {
            // 超出预算后进入“纯文本追加”模式，避免在超大日志上卡住 UI。
            _box.AppendText(StripInvisible(run.Text));
        }
    }

    /// <summary>
    /// 清空文本后，选择区属性会回到控件默认值，缓存必须一并失效，
    /// 否则同样的颜色会被认为“已经是该值”而跳过设置。
    /// </summary>
    private void ResetStyleCache()
    {
        _currentStyle = FontStyle.Regular;
        _currentForeground = default;
        _currentBackground = default;
    }

    private void SetFontStyle(FontStyle style)
    {
        if (style == _currentStyle)
        {
            return;
        }

        if (!_fonts.TryGetValue(style, out var font))
        {
            font = new Font(_box.Font, style);
            _fonts[style] = font;
        }

        _box.SelectionFont = font;
        _currentStyle = style;
    }

    private void ResetSelection()
    {
        _box.Select(_box.TextLength, 0);

        if (_currentForeground != _box.ForeColor)
        {
            _box.SelectionColor = _box.ForeColor;
            _currentForeground = _box.ForeColor;
        }

        if (_currentBackground != _box.BackColor)
        {
            _box.SelectionBackColor = _box.BackColor;
            _currentBackground = _box.BackColor;
        }

        SetFontStyle(FontStyle.Regular);
        _box.Select(_box.TextLength, 0);
    }

    /// <summary>去掉不可见控制符，用于放弃着色后的纯文本追加。</summary>
    private static string StripInvisible(string text)
    {
        if (!text.Any(char.IsControl))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is '\r' or '\n' or '\t' || !char.IsControl(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    /// <summary>从最旧的条目开始裁剪，直到回到上限以内。</summary>
    private void Trim()
    {
        var removeCount = Math.Min(_trimBatch, _entries.Count);
        var charsToRemove = 0;

        for (var i = 0; i < removeCount; i++)
        {
            charsToRemove += _charCounts[i];
        }

        _entries.RemoveRange(0, removeCount);
        _charCounts.RemoveRange(0, removeCount);

        // 按“字符”而不是“显示行”定位：开启自动换行后两者不再一一对应。
        if (charsToRemove <= 0)
        {
            return;
        }

        var readOnly = _box.ReadOnly;
        _box.ReadOnly = false;
        _box.Select(0, charsToRemove);
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

    private Color ColorFor(LogDirection direction) => LogColors.ForDirection(direction);
}