using System.Runtime.InteropServices;

namespace ClipShelf.Desktop.Platform.Linux;

// Sign-out and shutdown reach the app as SIGTERM. The host's console lifetime also catches
// SIGTERM and cancels the default exit, so after the cleanup runs this exits the process itself.
public sealed class LinuxSessionEnd : ISessionEnd, IDisposable
{
    private readonly Action<int> _exit;
    private readonly PosixSignalRegistration? _registration;

    public LinuxSessionEnd() : this(Environment.Exit)
    {
        _registration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => OnTerminate());
    }

    internal LinuxSessionEnd(Action<int> exit) => _exit = exit;

    public event EventHandler? Ending;

    internal void OnTerminate()
    {
        Ending?.Invoke(this, EventArgs.Empty);
        _exit(0);
    }

    public void Dispose() => _registration?.Dispose();
}
