using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace ClipShelf.Presentation.Hosting;

// One instance per name. The first process claims the pipe and listens; later processes connect,
// send one command line and exit. NamedPipeServerStream maps to a Unix domain socket off Windows.
public sealed class SingleInstanceService : IDisposable
{
    private readonly string _name;
    private readonly CancellationTokenSource _cts = new();
    private NamedPipeServerStream? _server;
    private FileStream? _lock;
    private bool _disposed;

    public SingleInstanceService(string name) => _name = name;

    // Scoped per user and per session: a machine-wide name would let one user's instance block
    // another user's launch, and a per-user-only name would merge two sessions of the same user
    // (RDP), where the old mutex kept them apart. Off Windows the session id is the POSIX one
    // (getsid), which differs between a terminal and a launcher, so only the user scopes the name.
    public static string DefaultName { get; } =
        "ClipShelf.SingleInstance." + Environment.UserName +
        (OperatingSystem.IsWindows()
            ? "." + Process.GetCurrentProcess().SessionId.ToString(CultureInfo.InvariantCulture)
            : "");

    private readonly Lock _gate = new();
    private readonly Queue<string> _undelivered = new();
    private EventHandler<string>? _commandReceived;

    // Raised on a worker thread. Commands that arrive before anyone listens (the app claims the
    // name in Main but subscribes once Avalonia has started) are kept and delivered on subscribe.
    public event EventHandler<string>? CommandReceived
    {
        add
        {
            string[] pending;
            lock (_gate)
            {
                _commandReceived += value;
                pending = [.. _undelivered];
                _undelivered.Clear();
            }
            foreach (var command in pending) value?.Invoke(this, command);
        }
        remove
        {
            lock (_gate) _commandReceived -= value;
        }
    }

    // Off Windows the pipe is a Unix domain socket that another process can re-bind, so an
    // exclusive lock file (flock, released by the kernel when the process dies) is the claim.
    internal static string LockPath(string name) => Endpoint(name) + ".lock";

    // The pipe's address. Off Windows it is a socket file, and macOS caps those paths at 104
    // characters while its per-user TMPDIR is long, so the file gets a short name hashed from the
    // full one, in the per-user runtime directory where there is one.
    internal static string Endpoint(string name)
    {
        if (OperatingSystem.IsWindows()) return name;
        var directory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime ? runtime : Path.GetTempPath();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16];
        return Path.Combine(directory, "clipshelf-" + hash.ToLowerInvariant());
    }

    // False when another instance already owns the name.
    public bool TryClaim()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                _lock = new FileStream(LockPath(_name), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _server = new NamedPipeServerStream(
                Endpoint(_name), PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _lock?.Dispose();
            _lock = null;
            return false;
        }

        _ = Task.Run(AcceptLoopAsync);
        return true;
    }

    // Connects to the running instance and sends one command; retries briefly so a first instance
    // that is still starting is found. False when nothing answers.
    public static async Task<bool> TrySendAsync(
        string name, string command, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(
                    ".", Endpoint(name), PipeDirection.Out, PipeOptions.Asynchronous);
                await client.ConnectAsync(50, cancellationToken);
                using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
                await writer.WriteLineAsync(command);
                return true;
            }
            catch (Exception ex) when (
                ex is IOException or TimeoutException or UnauthorizedAccessException
                    or System.Net.Sockets.SocketException)
            {
                // The server is not there yet; wait and try again.
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            try
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _server?.Dispose();
        _lock?.Dispose();
        _cts.Dispose();
    }

    private void Deliver(string command)
    {
        EventHandler<string>? handlers;
        lock (_gate)
        {
            handlers = _commandReceived;
            if (handlers is null) _undelivered.Enqueue(command);
        }
        handlers?.Invoke(this, command);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _server!.WaitForConnectionAsync(_cts.Token);
                using (var reader = new StreamReader(
                    _server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
                {
                    var command = await reader.ReadLineAsync(_cts.Token);
                    if (!string.IsNullOrEmpty(command))
                        Deliver(command);
                }

                _server.Disconnect();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (IOException)
            {
                // A client that disconnected early; keep listening.
            }
        }
    }
}
