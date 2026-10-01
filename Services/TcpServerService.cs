using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace ComServer.Services;

/// <summary>
/// TCP 串口服务器。
/// <para>
/// 监听 <see cref="IPAddress.Any"/>，允许多个客户端同时接入；
/// 每个客户端有独立的接收循环和独立的发送队列（队列满时丢弃并提示，避免一个卡死的客户端拖垮整个服务）。
/// </para>
/// 事件都在后台线程触发，调用方需要自己处理线程切换。
/// </summary>
public sealed class TcpServerService : IAsyncDisposable
{
    private const int BufferSize = 8192;
    private const int ClientQueueCapacity = 256;
    private const int StopTimeoutSeconds = 3;

    private readonly object _sync = new();
    private readonly ConcurrentDictionary<int, ClientConnection> _clients = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private int _nextClientId;

    /// <summary>新客户端接入：(编号, 远端地址)。</summary>
    public event Action<int, string>? ClientConnected;

    /// <summary>客户端断开：(编号, 远端地址)。</summary>
    public event Action<int, string>? ClientDisconnected;

    /// <summary>收到客户端数据：(编号, 数据)。返回的任务会被等待，从而保证写入串口的顺序与背压。</summary>
    public event Func<int, byte[], ValueTask>? DataReceived;

    /// <summary>运行期错误提示。</summary>
    public event Action<string>? ErrorOccurred;

    public bool IsRunning
    {
        get { lock (_sync) { return _listener is not null; } }
    }

    /// <summary>实际监听的端口（传 0 时由系统分配）。</summary>
    public int Port { get; private set; }

    public int ClientCount => _clients.Count;

    /// <summary>开始监听。端口被占用等情况会抛出异常，由界面层提示。</summary>
    public void Start(int port)
    {
        lock (_sync)
        {
            if (_listener is not null)
            {
                throw new InvalidOperationException("TCP 服务已经启动。");
            }

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            var cts = new CancellationTokenSource();
            _listener = listener;
            _cts = cts;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _acceptTask = Task.Run(() => AcceptLoopAsync(listener, cts.Token));
        }
    }

    /// <summary>停止监听并断开所有客户端。</summary>
    public async Task StopAsync()
    {
        TcpListener? listener;
        CancellationTokenSource? cts;
        Task? acceptTask;
        ClientConnection[] connections;

        lock (_sync)
        {
            listener = _listener;
            cts = _cts;
            acceptTask = _acceptTask;

            _listener = null;
            _cts = null;
            _acceptTask = null;

            connections = [.. _clients.Values];
        }

        if (listener is null)
        {
            return;
        }

        try { listener.Stop(); } catch { /* 已停止 */ }
        try { cts?.Cancel(); } catch { /* 已释放 */ }

        // 保证 accept 循环即使被唤醒也不会再登记新客户端。
        try { await (acceptTask ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(StopTimeoutSeconds)).ConfigureAwait(false); }
        catch { }

        foreach (var connection in connections)
        {
            connection.RequestClose();
        }

        var drains = new List<Task>(connections.Length);
        foreach (var connection in connections)
        {
            drains.Add(connection.Task);
        }

        try { await Task.WhenAll(drains).WaitAsync(TimeSpan.FromSeconds(StopTimeoutSeconds)).ConfigureAwait(false); }
        catch { }

        foreach (var connection in connections)
        {
            _clients.TryRemove(connection.Id, out _);
            connection.Dispose();
        }

        cts?.Dispose();
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    /// <summary>
    /// 把数据发给所有已连接的客户端（串口 → TCP 方向）。
    /// 这是热路径，非阻塞：队列已满的客户端会被跳过并提示一次。
    /// </summary>
    public void Broadcast(byte[] data)
    {
        if (data.Length == 0 || _clients.IsEmpty)
        {
            return;
        }

        foreach (var connection in _clients.Values)
        {
            if (connection.TryEnqueue(data))
            {
                continue;
            }

            if (connection.MarkOverflowNotified())
            {
                ErrorOccurred?.Invoke($"客户端 #{connection.Id} 接收过慢，已丢弃部分数据。");
            }
        }
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                // 单个握手失败不应该让整个监听退出。
                ErrorOccurred?.Invoke($"接受连接失败：{ex.Message}");
                continue;
            }

            var id = Interlocked.Increment(ref _nextClientId);
            var connection = new ClientConnection(id, client, token);
            _clients[id] = connection;
            connection.Task = HandleClientAsync(connection);
        }
    }

    private async Task HandleClientAsync(ClientConnection connection)
    {
        ClientConnected?.Invoke(connection.Id, connection.Remote);

        try
        {
            var writer = WriteLoopAsync(connection);
            await ReadLoopAsync(connection).ConfigureAwait(false);

            connection.RequestClose();
            connection.CompleteOutbound();

            try { await writer.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false); } catch { }
        }
        finally
        {
            _clients.TryRemove(connection.Id, out _);
            connection.Dispose();
            ClientDisconnected?.Invoke(connection.Id, connection.Remote);
        }
    }

    private async Task ReadLoopAsync(ClientConnection connection)
    {
        var token = connection.Cancellation.Token;
        var buffer = new byte[BufferSize];

        try
        {
            var stream = connection.Client.GetStream();

            while (!token.IsCancellationRequested)
            {
                var count = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                if (count <= 0)
                {
                    break; // 对端正常关闭。
                }

                var handler = DataReceived;
                if (handler is not null)
                {
                    // 等待处理完成：串口写入队列满时自然形成背压，不会丢弃数据。
                    await handler(connection.Id, buffer[..count]).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }          // 连接被重置
        catch (SocketException) { }
        catch (ObjectDisposedException) { }
    }

    private static async Task WriteLoopAsync(ClientConnection connection)
    {
        try
        {
            var stream = connection.Client.GetStream();

            await foreach (var chunk in connection.Outbound.Reader.ReadAllAsync(connection.Cancellation.Token).ConfigureAwait(false))
            {
                await stream.WriteAsync(chunk, connection.Cancellation.Token).ConfigureAwait(false);
                connection.ResetOverflow();
            }

            await stream.FlushAsync(connection.Cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (SocketException) { }
        catch (ObjectDisposedException) { }
        catch (ChannelClosedException) { }
    }

    /// <summary>一个 TCP 客户端连接的运行时状态。</summary>
    private sealed class ClientConnection
    {
        private int _overflowNotified;

        public ClientConnection(int id, TcpClient client, CancellationToken serverToken)
        {
            Id = id;
            Client = client;
            Cancellation = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
            Remote = DescribeRemote(client);

            try
            {
                // 串口数据包通常很小，禁用延迟确认能明显降低往返延迟。
                client.NoDelay = true;
            }
            catch { }

            Outbound = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(ClientQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
        }

        public int Id { get; }

        public TcpClient Client { get; }

        public string Remote { get; }

        public Channel<byte[]> Outbound { get; }

        public CancellationTokenSource Cancellation { get; }

        public Task Task { get; set; } = System.Threading.Tasks.Task.CompletedTask;

        /// <summary>非阻塞入队；队列满时返回 <c>false</c>，由调用方决定如何提示。</summary>
        public bool TryEnqueue(byte[] data) => Outbound.Writer.TryWrite(data);

        public bool MarkOverflowNotified() => Interlocked.Exchange(ref _overflowNotified, 1) == 0;

        public void ResetOverflow() => Interlocked.Exchange(ref _overflowNotified, 0);

        public void CompleteOutbound() => Outbound.Writer.TryComplete();

        public void RequestClose()
        {
            try { Cancellation.Cancel(); } catch { }
            CompleteOutbound();
            try { Client.Close(); } catch { }
        }

        public void Dispose()
        {
            try { Client.Dispose(); } catch { }
            Cancellation.Dispose();
        }

        private static string DescribeRemote(TcpClient client)
        {
            try
            {
                return client.Client.RemoteEndPoint?.ToString() ?? "未知地址";
            }
            catch
            {
                return "未知地址";
            }
        }
    }
}
