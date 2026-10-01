using System.IO.Ports;
using System.Text;
using System.Threading.Channels;

namespace ComServer.Services;

/// <summary>串口连接参数。</summary>
/// <param name="PortName">串口名，例如 <c>COM3</c>。</param>
public sealed record SerialPortSettings(string PortName, int BaudRate, Parity Parity, int DataBits, StopBits StopBits)
{
    public override string ToString()
    {
        var parity = Parity switch
        {
            System.IO.Ports.Parity.None => "N",
            System.IO.Ports.Parity.Odd => "O",
            System.IO.Ports.Parity.Even => "E",
            System.IO.Ports.Parity.Mark => "M",
            _ => "S",
        };

        var stop = StopBits switch
        {
            System.IO.Ports.StopBits.One => "1",
            System.IO.Ports.StopBits.OnePointFive => "1.5",
            System.IO.Ports.StopBits.Two => "2",
            _ => "?",
        };

        return $"{PortName} @{BaudRate}-{DataBits}{parity}{stop}";
    }
}

/// <summary>
/// 串口读写封装。
/// <para>
/// 读取用一个后台循环（阻塞读 + 100ms 超时，便于响应取消，也不占用 CPU）；
/// 写入用一个容量有限的队列串行化，避免多线程同时写同一个 <see cref="SerialPort"/>。
/// 队列满时调用方会自然被挂起，形成对 TCP 客户端的背压，而不是丢数据。
/// </para>
/// 事件都在后台线程触发，调用方需要自己处理线程切换。
/// </summary>
public sealed class SerialPortService : IDisposable
{
    private const int BufferSize = 4096;
    private const int ReadTimeoutMs = 100;
    private const int WriteTimeoutMs = 3000;
    private const int WriteQueueCapacity = 256;

    private readonly object _sync = new();
    private SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Channel<byte[]>? _writeQueue;
    private Task? _readTask;
    private Task? _writeTask;

    /// <summary>从串口读到的数据（串口 → TCP 方向）。</summary>
    public event Action<byte[]>? DataReceived;

    /// <summary>读写过程中出现的错误提示。</summary>
    public event Action<string>? ErrorOccurred;

    /// <summary>串口异常断开（例如 USB 转串口被拔出）。</summary>
    public event Action? Faulted;

    public bool IsOpen
    {
        get { lock (_sync) { return _port is not null; } }
    }

    public string? PortName
    {
        get { lock (_sync) { return _port?.PortName; } }
    }

    /// <summary>打开串口。失败时抛出异常，由界面层展示友好的提示。</summary>
    public void Open(SerialPortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_sync)
        {
            if (_port is not null)
            {
                throw new InvalidOperationException("串口已经打开。");
            }

            var port = new SerialPort(settings.PortName, settings.BaudRate, settings.Parity, settings.DataBits, settings.StopBits)
            {
                // -1 表示无限等待；这里用短超时是为了让读循环能定期检查取消。
                ReadTimeout = ReadTimeoutMs,
                WriteTimeout = WriteTimeoutMs,
                ReadBufferSize = BufferSize,
                WriteBufferSize = BufferSize,
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true,
                Encoding = Encoding.UTF8,
            };

            port.Open();

            var cts = new CancellationTokenSource();
            var queue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(WriteQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });

            _port = port;
            _cts = cts;
            _writeQueue = queue;
            _readTask = Task.Run(() => ReadLoop(port, cts.Token));
            _writeTask = Task.Run(() => WriteLoopAsync(port, queue, cts.Token));
        }
    }

    /// <summary>关闭串口并等待后台循环退出；重复调用是安全的。</summary>
    public void Close()
    {
        SerialPort? port;
        CancellationTokenSource? cts;
        Task? readTask;
        Task? writeTask;
        Channel<byte[]>? queue;

        lock (_sync)
        {
            port = _port;
            if (port is null)
            {
                return;
            }

            cts = _cts;
            readTask = _readTask;
            writeTask = _writeTask;
            queue = _writeQueue;

            _port = null;
            _cts = null;
            _readTask = null;
            _writeTask = null;
            _writeQueue = null;
        }

        try { cts?.Cancel(); } catch { /* 已释放 */ }
        queue?.Writer.TryComplete();

        var tasks = new List<Task>(2);
        if (readTask is not null) tasks.Add(readTask);
        if (writeTask is not null) tasks.Add(writeTask);

        try { Task.WaitAll([.. tasks], TimeSpan.FromSeconds(2)); } catch { /* 超时或任务异常，继续清理 */ }

        try { port.Close(); } catch { /* 设备可能已被拔出 */ }
        try { port.Dispose(); } catch { }
        cts?.Dispose();
    }

    /// <summary>把数据排入串口发送队列。串口未打开时静默忽略。</summary>
    public async ValueTask SendAsync(byte[] data)
    {
        if (data.Length == 0)
        {
            return;
        }

        Channel<byte[]>? queue;
        lock (_sync)
        {
            queue = _writeQueue;
        }

        if (queue is null)
        {
            return;
        }

        try
        {
            await queue.Writer.WriteAsync(data).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            // 串口正在关闭，丢弃即可。
        }
    }

    public void Dispose() => Close();

    private void ReadLoop(SerialPort port, CancellationToken token)
    {
        var buffer = new byte[BufferSize];
        var faulted = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                int count;
                try
                {
                    count = port.Read(buffer, 0, buffer.Length);
                }
                catch (TimeoutException)
                {
                    continue; // 正常情况：这段时间没有数据。
                }

                if (count > 0)
                {
                    DataReceived?.Invoke(buffer[..count]);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 主动关闭时端口被关闭也会走到这里，用取消标记区分。
            if (!token.IsCancellationRequested)
            {
                faulted = true;
                ErrorOccurred?.Invoke($"串口 {port.PortName} 读取失败：{ex.Message}");
            }
        }
        finally
        {
            try { port.Close(); } catch { }
            if (faulted)
            {
                Faulted?.Invoke();
            }
        }
    }

    private async Task WriteLoopAsync(SerialPort port, Channel<byte[]> queue, CancellationToken token)
    {
        try
        {
            await foreach (var chunk in queue.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                try
                {
                    port.Write(chunk, 0, chunk.Length);
                    port.BaseStream.Flush(); // 立即下发，降低转发延迟。
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (!token.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke($"串口 {port.PortName} 写入失败：{ex.Message}");
                    }

                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭。
        }
    }
}
