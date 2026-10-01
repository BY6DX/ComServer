using System.Collections.Concurrent;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ComServer.Models;
using ComServer.Rendering;
using ComServer.Services;
using ComServer.Utilities;

namespace ComServer;

/// <summary>
/// 串口服务器的界面与编排层。
/// <para>
/// 线程模型：串口与 TCP 服务都在后台线程触发事件，所有日志先进入无锁队列，
/// 再由一个 50ms 的 UI 定时器统一排空；因此高频数据不会把窗口消息队列打满。
/// 状态切换（启动 / 停止 / 串口掉线）频率很低，直接切回 UI 线程处理。
/// </para>
/// </summary>
public partial class Form1 : Form
{
    private const int MaxLogEntries = 5000;
    private const int MaxEntriesPerDrain = 3000;
    private const int LogDrainIntervalMs = 50;
    private const int StatsIntervalMs = 250;

    private readonly SerialPortService _serial = new();
    private readonly TcpServerService _server = new();
    private readonly ConcurrentQueue<LogEntry> _pendingLogs = new();
    private readonly System.Windows.Forms.Timer _logTimer = new();
    private readonly System.Windows.Forms.Timer _statsTimer = new();

    private LogView _logView = null!;
    private readonly Encoding _sendEncoding = new UTF8Encoding(false);

    private long _rxBytes;
    private long _txBytes;
    private int _pendingCount;
    private int _clientCount;
    private bool _running;
    private bool _closing;
    private string _endpointHint = "127.0.0.1";

    public Form1()
    {
        InitializeComponent();
        BuildUiOptions();
        WireEvents();

        _logView = new LogView(_logBox, MaxLogEntries) { AutoScroll = _autoScrollCheck.Checked };

        _logTimer.Interval = LogDrainIntervalMs;
        _logTimer.Tick += (_, _) => DrainLogQueue();

        _statsTimer.Interval = StatsIntervalMs;
        _statsTimer.Tick += (_, _) => UpdateStatus();
    }

    #region 初始化

    private void BuildUiOptions()
    {
        _baudCombo.Items.AddRange(["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600"]);
        _baudCombo.Text = "115200";

        _dataBitsCombo.Items.AddRange(["5", "6", "7", "8"]);
        _dataBitsCombo.SelectedIndex = 3;

        // 下拉顺序与 StopBits / Parity 的枚举顺序一致，避免额外的映射表。
        _stopBitsCombo.Items.AddRange(["1", "1.5", "2"]);
        _stopBitsCombo.SelectedIndex = 0;

        _parityCombo.Items.AddRange(["无 (None)", "奇 (Odd)", "偶 (Even)", "标记 (Mark)", "空格 (Space)"]);
        _parityCombo.SelectedIndex = 0;

        _sendModeCombo.Items.AddRange(["文本", "HEX"]);
        _sendModeCombo.SelectedIndex = 0;

        _encodingCombo.Items.AddRange(
        [
            new EncodingItem("UTF-8", new UTF8Encoding(false)),
            new EncodingItem("GB18030", GetEncodingOrFallback("GB18030")),
            new EncodingItem("ASCII", Encoding.ASCII),
            new EncodingItem("Latin1", Encoding.Latin1),
        ]);
        _encodingCombo.SelectedIndex = 0;

        _toolTip.SetToolTip(_hexCheck, "按十六进制显示数据；切换后会立即重排已有日志。");
        _toolTip.SetToolTip(_autoScrollCheck, "新数据到达时自动滚动到底部；手动向上滚动时会暂停。");
        _toolTip.SetToolTip(_sendButton, "把输入内容写入串口（不发送给 TCP 客户端）。");
    }

    private void WireEvents()
    {
        _refreshButton.Click += (_, _) => RefreshPortList();
        _startButton.Click += async (_, _) => await ToggleServerAsync();
        _portCombo.DropDown += (_, _) => RefreshPortList();

        _hexCheck.CheckedChanged += (_, _) =>
        {
            _logView.Options.Mode = _hexCheck.Checked ? PayloadDisplayMode.Hex : PayloadDisplayMode.Text;
            _logView.RefreshFormatting();
        };

        _timestampCheck.CheckedChanged += (_, _) =>
        {
            _logView.Options.ShowTimestamp = _timestampCheck.Checked;
            _logView.RefreshFormatting();
        };

        _autoScrollCheck.CheckedChanged += (_, _) => _logView.AutoScroll = _autoScrollCheck.Checked;

        _encodingCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_encodingCombo.SelectedItem is EncodingItem item)
            {
                _logView.Options.TextEncoding = item.Value;
                _logView.RefreshFormatting();
            }
        };

        _clearButton.Click += (_, _) => _logView.Clear();
        _saveButton.Click += (_, _) => SaveLog();
        _sendButton.Click += async (_, _) => await SendManualAsync();
        _sendBox.KeyDown += async (_, e) => await OnSendBoxKeyDownAsync(e);

        _serial.DataReceived += OnSerialData;
        _serial.ErrorOccurred += message => Enqueue(LogEntry.Error(DateTime.Now, message));
        _serial.Faulted += OnSerialFaulted;

        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.DataReceived += OnTcpData;
        _server.ErrorOccurred += message => Enqueue(LogEntry.Error(DateTime.Now, message));

        Shown += (_, _) =>
        {
            RefreshPortList();
            _logTimer.Start();
            _statsTimer.Start();
            Enqueue(LogEntry.System(DateTime.Now, "就绪。选择串口与监听端口后点击“启动”。"));
        };

        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            _logTimer.Dispose();
            _statsTimer.Dispose();
        };
    }

    #endregion

    #region 串口 / TCP 事件（后台线程）

    /// <summary>串口 → TCP：广播给所有客户端。</summary>
    private void OnSerialData(byte[] data)
    {
        Enqueue(new LogEntry(DateTime.Now, LogDirection.Rx, data));
        Interlocked.Add(ref _rxBytes, data.Length);
        _server.Broadcast(data);
    }

    /// <summary>TCP → 串口：写入串口发送队列（等待入队，天然形成背压）。</summary>
    private async ValueTask OnTcpData(int clientId, byte[] data)
    {
        Enqueue(new LogEntry(DateTime.Now, LogDirection.Tx, data, $"#{clientId}"));
        Interlocked.Add(ref _txBytes, data.Length);
        await _serial.SendAsync(data).ConfigureAwait(false);
    }

    private void OnClientConnected(int id, string remote)
    {
        Interlocked.Increment(ref _clientCount);
        Enqueue(LogEntry.System(DateTime.Now, $"客户端 #{id} 已连接（{remote}）"));
    }

    private void OnClientDisconnected(int id, string remote)
    {
        Interlocked.Decrement(ref _clientCount);
        Enqueue(LogEntry.System(DateTime.Now, $"客户端 #{id} 已断开（{remote}）"));
    }

    private void OnSerialFaulted()
    {
        Post(async () =>
        {
            Enqueue(LogEntry.Error(DateTime.Now, "串口连接已断开，服务将停止。"));
            if (_running)
            {
                await StopServerAsync();
            }
        });
    }

    #endregion

    #region 启动 / 停止

    private async Task ToggleServerAsync()
    {
        if (_running)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    private async Task StartServerAsync()
    {
        if (!TryBuildSettings(out var settings, out var error))
        {
            MessageBox.Show(this, error, "参数有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tcpPort = (int)_tcpPortBox.Value;

        _startButton.Enabled = false;
        _statusLabel.Text = "正在启动…";

        try
        {
            _server.Start(tcpPort);

            try
            {
                _serial.Open(settings);
            }
            catch
            {
                await _server.StopAsync();
                throw;
            }

            Interlocked.Exchange(ref _rxBytes, 0);
            Interlocked.Exchange(ref _txBytes, 0);
            _clientCount = 0;
            _running = true;

            SetConfigurationEnabled(false);
            _startButton.Text = "停止";
            _endpointHint = DescribeLocalAddresses();

            Enqueue(LogEntry.System(DateTime.Now, $"服务已启动：TCP 0.0.0.0:{_server.Port} ⇄ {settings}"));
            Enqueue(LogEntry.System(DateTime.Now, $"客户端可连接到：{_endpointHint}:{_server.Port}"));
        }
        catch (Exception ex)
        {
            Enqueue(LogEntry.Error(DateTime.Now, $"启动失败：{ex.Message}"));
            MessageBox.Show(this, $"启动失败：{ex.Message}", "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "已停止";
        }
        finally
        {
            _startButton.Enabled = true;
            UpdateStatus();
        }
    }

    private async Task StopServerAsync()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        _startButton.Enabled = false;
        _startButton.Text = "停止中…";

        try
        {
            await _server.StopAsync();

            // 串口关闭内部会等待读循环退出，放到线程池避免卡住界面。
            await Task.Run(_serial.Close);
        }
        catch (Exception ex)
        {
            Enqueue(LogEntry.Error(DateTime.Now, $"停止时出错：{ex.Message}"));
        }
        finally
        {
            _clientCount = 0;
            _startButton.Text = "启动";
            _startButton.Enabled = true;
            SetConfigurationEnabled(true);
            Enqueue(LogEntry.System(DateTime.Now, "服务已停止。"));
            UpdateStatus();
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closing || !_running)
        {
            return;
        }

        // 先在后台把监听与串口干净地关掉，再真正关闭窗口。
        e.Cancel = true;
        _closing = true;
        _logTimer.Stop();
        _statsTimer.Stop();

        await StopServerAsync();
        Close();
    }

    private void SetConfigurationEnabled(bool enabled)
    {
        _portCombo.Enabled = enabled;
        _refreshButton.Enabled = enabled;
        _baudCombo.Enabled = enabled;
        _dataBitsCombo.Enabled = enabled;
        _stopBitsCombo.Enabled = enabled;
        _parityCombo.Enabled = enabled;
        _tcpPortBox.Enabled = enabled;
    }

    #endregion

    #region 手动发送

    private async Task OnSendBoxKeyDownAsync(KeyEventArgs e)
    {
        // 回车发送；Ctrl / Shift + 回车用于输入换行。
        if (e.KeyCode != Keys.Enter || e.Control || e.Shift)
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;
        await SendManualAsync();
    }

    private async Task SendManualAsync()
    {
        if (!_serial.IsOpen)
        {
            MessageBox.Show(this, "串口尚未打开，请先启动服务。", "无法发送", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var text = _sendBox.Text;
        byte[] payload;

        if (_sendModeCombo.SelectedIndex == 1)
        {
            if (!PayloadFormatter.TryParseHex(text, out payload))
            {
                MessageBox.Show(this, "HEX 内容无法解析，请使用形如 01 A2 FF 的格式。", "格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        else
        {
            payload = _sendEncoding.GetBytes(text);
        }

        if (payload.Length == 0)
        {
            return;
        }

        try
        {
            await _serial.SendAsync(payload);
            Enqueue(new LogEntry(DateTime.Now, LogDirection.Tx, payload, "手动"));
            Interlocked.Add(ref _txBytes, payload.Length);
            _sendBox.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"发送失败：{ex.Message}", "发送失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region 日志与状态

    private void Enqueue(LogEntry entry)
    {
        Interlocked.Increment(ref _pendingCount);
        _pendingLogs.Enqueue(entry);
    }

    /// <summary>在 UI 线程上批量排空日志队列。</summary>
    private void DrainLogQueue()
    {
        var drained = 0;
        while (drained < MaxEntriesPerDrain && _pendingLogs.TryDequeue(out var entry))
        {
            Interlocked.Decrement(ref _pendingCount);
            _logView.Add(entry);
            drained++;
        }

        // 还有积压时尽快再排一次，避免定时器间隔成为吞吐瓶颈。
        _logTimer.Interval = Volatile.Read(ref _pendingCount) > 0 ? 1 : LogDrainIntervalMs;
    }

    private void UpdateStatus()
    {
        _portStatusLabel.Text = _serial.IsOpen ? $"串口：{_serial.PortName}" : "串口：未打开";
        _clientStatusLabel.Text = $"客户端：{Volatile.Read(ref _clientCount)}";
        _rxStatusLabel.Text = $"RX：{ByteSize.Format(Interlocked.Read(ref _rxBytes))}";
        _txStatusLabel.Text = $"TX：{ByteSize.Format(Interlocked.Read(ref _txBytes))}";

        if (_running)
        {
            _statusLabel.Text = $"运行中 —— TCP 监听 0.0.0.0:{_server.Port}，本机地址 {_endpointHint}";
        }
        else if (_startButton.Text is "启动" or "停止中…")
        {
            _statusLabel.Text = "已停止";
        }
    }

    private void RefreshPortList()
    {
        var previous = _portCombo.SelectedItem as SerialPortInfo?;
        var ports = PortEnumerator.GetPorts();

        _portCombo.BeginUpdate();
        _portCombo.Items.Clear();
        foreach (var port in ports)
        {
            _portCombo.Items.Add(port);
        }

        _portCombo.EndUpdate();

        if (ports.Count == 0)
        {
            _portCombo.SelectedIndex = -1;
            return;
        }

        // 优先恢复原先选中的串口，其次选第一个。
        var index = 0;
        if (previous is { } current)
        {
            for (var i = 0; i < ports.Count; i++)
            {
                if (string.Equals(ports[i].PortName, current.PortName, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
        }

        _portCombo.SelectedIndex = index;
    }

    private void SaveLog()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            FileName = $"串口日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            AddExtension = true,
            DefaultExt = "txt",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _logView.Save(dialog.FileName);
            Enqueue(LogEntry.System(DateTime.Now, $"日志已保存到 {dialog.FileName}"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"保存失败：{ex.Message}", "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool TryBuildSettings(out SerialPortSettings settings, out string error)
    {
        settings = null!;
        error = string.Empty;

        // 列表项保存的是 SerialPortInfo，下拉框里显示的则是“COM3 — 描述”这样的文本。
        var portName = _portCombo.SelectedItem is SerialPortInfo info
            ? info.PortName
            : _portCombo.Text.Trim();

        if (portName.Length == 0)
        {
            error = "未检测到可用串口，请插入设备后点击“刷新”。";
            return false;
        }

        if (!int.TryParse(_baudCombo.Text.Trim(), out var baudRate) || baudRate <= 0)
        {
            error = "波特率必须是大于 0 的整数。";
            return false;
        }

        var dataBits = _dataBitsCombo.SelectedIndex >= 0 ? 5 + _dataBitsCombo.SelectedIndex : 8;
        var stopBits = _stopBitsCombo.SelectedIndex switch
        {
            1 => StopBits.OnePointFive,
            2 => StopBits.Two,
            _ => StopBits.One,
        };
        var parity = _parityCombo.SelectedIndex switch
        {
            1 => Parity.Odd,
            2 => Parity.Even,
            3 => Parity.Mark,
            4 => Parity.Space,
            _ => Parity.None,
        };

        settings = new SerialPortSettings(portName, baudRate, parity, dataBits, stopBits);
        return true;
    }

    private void Post(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
            // 窗口正在关闭。
        }
    }

    private static string DescribeLocalAddresses()
    {
        try
        {
            var addresses = Dns.GetHostAddresses(Dns.GetHostName())
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .ToArray();

            return addresses.Length > 0 ? string.Join(" / ", addresses) : "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    private static Encoding GetEncodingOrFallback(string name)
    {
        try
        {
            return Encoding.GetEncoding(name);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    /// <summary>日志编码下拉项：显示名称，取值是编码实例。</summary>
    private sealed record EncodingItem(string Name, Encoding Value)
    {
        public override string ToString() => Name;
    }

    #endregion
}
