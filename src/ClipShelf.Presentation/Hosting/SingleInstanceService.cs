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
    private bool _disposed;

    public SingleInstanceService(string name) => _name = name;

    // Scoped per user and per session: a machine-wide name would let one user's instance block
    // another user's launch, and a per-user-only name would merge two sessions of the same user
    // (RDP), where the old mutex kept them apart. The session id is 0 off Windows, where it is
    // stable for every process of a user.
    public static string DefaultName { get; } =
        "ClipShelf.SingleInstance." + Environment.UserName + "." +
        Process.GetCurrentProcess().SessionId.ToString(CultureInfo.InvariantCulture);

    public event EventHandler<string>? CommandReceived;

    // False when another instance already owns the name.
    public bool TryClaim()
    {
        try
        {
            _server = new NamedPipeServerStream(
                _name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
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
                    ".", name, PipeDirection.Out, PipeOptions.Asynchronous);
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
        _cts.Dispose();
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
                        CommandReceived?.Invoke(this, command);
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
