using System.Collections.Concurrent;
using System.Runtime.Versioning;
using System.Text;
using ClipShelf.Core.Services;
using ClipShelf.Desktop.Imaging;

namespace ClipShelf.Desktop.Platform.Linux;

// X11 clipboard watch, reads and serving. One pump thread owns the display
// (Xlib is not thread-safe); public calls marshal onto it. Owner changes on
// CLIPBOARD raise Changed with the same self-write counting as the Windows
// service, and capture runs off the pump thread. Without an X display the pump
// exits and the service stays inert so the app still boots.
[SupportedOSPlatform("linux")]
public sealed class X11ClipboardService : IClipboardMonitor, IDisposable
{
    private const int ThumbnailMaxEdge = 256;
    private const int PumpPollMs = 20;
    private const int ReadTimeoutMs = 5000;

    private readonly ClipCaptureService _capture;
    private readonly IClipRepository _repository;
    private readonly SettingsService _settings;
    private readonly IForegroundApp _foregroundApp;
    private readonly ILogger<X11ClipboardService> _log;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _pump;
    private readonly object _readLock = new();
    private IntPtr _display;
    private IntPtr _window;
    private IntPtr _clipboard;
    private IntPtr _targets;
    private IntPtr _utf8;
    private IntPtr _ansi;
    private IntPtr _text;
    private IntPtr _png;
    private IntPtr _incr;
    private IntPtr _property;
    private int _xfixesBase;
    private int _selfWrites;
    private string? _ownedText;
    private byte[]? _ownedImage;
    private PendingRead? _pending;
    private bool _disposed;

    public X11ClipboardService(
        ClipCaptureService capture,
        IClipRepository repository,
        SettingsService settings,
        IForegroundApp foregroundApp,
        ILogger<X11ClipboardService> log)
    {
        _capture = capture;
        _repository = repository;
        _settings = settings;
        _foregroundApp = foregroundApp;
        _log = log;
        _pump = new Thread(Pump) { IsBackground = true, Name = "X11Clipboard" };
        if (OperatingSystem.IsLinux()) _pump.Start();
    }

    public event EventHandler? Changed;

    public string? ReadText()
    {
        try
        {
            var bytes = ReadTarget(X11Selection.Utf8) ?? ReadTarget(X11Selection.Ansi);
            return bytes is null ? null : Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Reading text from the X11 clipboard failed.");
            return null;
        }
    }

    public byte[]? ReadImage()
    {
        try
        {
            return ReadTarget(X11Selection.PngMime);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Reading an image from the X11 clipboard failed.");
            return null;
        }
    }

    public void WriteText(string text)
    {
        try
        {
            Invoke(() =>
            {
                if (!OwnSelection()) return false;
                _ownedText = text;
                _ownedImage = null;
                _selfWrites++;
                return true;
            });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Writing text to the X11 clipboard failed.");
        }
    }

    public Task WriteImageAsync(byte[] png)
    {
        try
        {
            var owned = Invoke(() =>
            {
                if (!OwnSelection()) return false;
                _ownedText = null;
                _ownedImage = png;
                _selfWrites++;
                return true;
            });
            if (!owned) _log.LogWarning("Taking clipboard ownership failed.");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Writing an image to the X11 clipboard failed.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
        if (OperatingSystem.IsLinux() && _pump.IsAlive) _pump.Join(TimeSpan.FromSeconds(2));
        _queue.Dispose();
    }

    // Runs an action on the pump thread, or inline when already there.
    private T Invoke<T>(Func<T> work, int timeoutMs = ReadTimeoutMs)
    {
        if (Thread.CurrentThread == _pump)
            return work();

        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _queue.Add(() =>
            {
                try
                {
                    done.SetResult(work());
                }
                catch (Exception ex)
                {
                    done.SetException(ex);
                }
            });
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("The X11 clipboard pump is not running.");
        }

        if (!done.Task.Wait(timeoutMs))
            throw new TimeoutException("The X11 clipboard did not answer in time.");
        return done.Task.GetAwaiter().GetResult();
    }

    private void Pump()
    {
        try
        {
            Open();
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is InvalidOperationException)
        {
            _log.LogWarning(ex, "No X display; clipboard capture is unavailable.");
            FailQueued();
            return;
        }

        try
        {
            var evt = new byte[X11Selection.EventSize];
            while (!_disposed)
            {
                while (_queue.TryTake(out var work, 0)) work();
                if (_disposed) break;
                if (X11Native.XPending(_display) > 0)
                {
                    X11Native.XNextEvent(_display, evt);
                    OnEvent(evt);
                    continue;
                }

                Thread.Sleep(PumpPollMs);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "The X11 clipboard pump failed.");
        }
        finally
        {
            FailQueued();
            if (_display != IntPtr.Zero) X11Native.XCloseDisplay(_display);
        }
    }

    private void FailQueued()
    {
        _queue.CompleteAdding();
    }

    private void Open()
    {
        _display = X11Native.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException("XOpenDisplay returned no display.");

        var root = X11Native.XDefaultRootWindow(_display);
        _window = X11Native.XCreateSimpleWindow(_display, root, 0, 0, 1, 1, 0, 0, 0);
        _clipboard = X11Native.XInternAtom(_display, "CLIPBOARD", false);
        _targets = X11Native.XInternAtom(_display, X11Selection.Targets, false);
        _utf8 = X11Native.XInternAtom(_display, X11Selection.Utf8, false);
        _ansi = X11Native.XInternAtom(_display, X11Selection.Ansi, false);
        _text = X11Native.XInternAtom(_display, X11Selection.Text, false);
        _png = X11Native.XInternAtom(_display, X11Selection.PngMime, false);
        _incr = X11Native.XInternAtom(_display, X11Selection.Incr, false);
        _property = X11Native.XInternAtom(_display, "CLIPSHELF_SEL", false);

        if (!X11Native.XFixesQueryExtension(_display, out _xfixesBase, out _))
            throw new InvalidOperationException("XFixes is unavailable.");
        X11Native.XFixesSelectSelectionInput(
            _display, root, _clipboard, X11Native.XFixesSelectionOwnerNotifyMask);
        X11Native.XFlush(_display);
    }

    private void OnEvent(byte[] evt)
    {
        var type = X11Selection.TypeOf(evt);
        if (type == _xfixesBase + X11Native.XFixesSelectionOwnerNotify)
        {
            OnOwnerChanged(evt);
            return;
        }

        switch (type)
        {
            case X11Selection.SelectionNotify:
                OnSelectionNotify(evt);
                break;
            case X11Selection.SelectionRequest:
                OnSelectionRequest(evt);
                break;
            case X11Selection.SelectionClear:
                _ownedText = null;
                _ownedImage = null;
                break;
        }
    }

    private void OnOwnerChanged(byte[] evt)
    {
        if (X11Selection.SelectionOf(evt) != _clipboard) return;
        if (X11Selection.OwnerOf(evt) == _window)
        {
            // Our own TakeSelection trips the watch; pair it with the write.
            if (_selfWrites > 0) _selfWrites--;
            return;
        }

        if (_selfWrites > 0)
        {
            _selfWrites--;
            return;
        }

        Changed?.Invoke(this, EventArgs.Empty);

        // The source app is read now, while the copy is fresh, off the pump thread.
        var appName = _foregroundApp.Name();
        _ = CaptureAsync(appName);
    }

    private void OnSelectionNotify(byte[] evt)
    {
        PendingRead? pending;
        lock (_readLock)
        {
            pending = _pending;
            _pending = null;
        }

        if (pending is null) return;
        if (X11Selection.TryParseSelectionNotify(evt, _window, _clipboard, out _, out var property))
            pending.Bytes = X11Native.ReadBytes(_display, _window, property, _incr);
        pending.Done.SetResult(true);
    }

    private void OnSelectionRequest(byte[] evt)
    {
        if (!X11Selection.TryParseSelectionRequest(evt, out var requestor, out var selection,
            out var target, out var property))
            return;
        if (selection != _clipboard) return;
        if (property == X11Native.None) property = target;

        var served = Serve(requestor, target, property);
        var reply = X11Selection.BuildSelectionResponse(
            _display, requestor, selection, target, served ? property : X11Native.None);
        X11Native.XSendEvent(_display, requestor, false, IntPtr.Zero, reply);
        X11Native.XFlush(_display);
    }

    // Answers a request from our owned payload. Unknown targets are refused.
    private bool Serve(IntPtr requestor, IntPtr target, IntPtr property)
    {
        if (target == _targets)
        {
            var names = X11Selection.ServeTargets(_ownedImage is not null);
            var atoms = names.Select(name => X11Native.XInternAtom(_display, name, false)).ToArray();
            var data = new byte[atoms.Length * IntPtr.Size];
            for (var i = 0; i < atoms.Length; i++)
                BitConverter.GetBytes(atoms[i].ToInt64()).CopyTo(data, i * IntPtr.Size);
            X11Native.XChangeProperty(_display, requestor, property, target, 32,
                X11Native.PropModeReplace, data, atoms.Length);
            return true;
        }

        if ((target == _utf8 || target == _ansi || target == _text) && _ownedText is not null)
        {
            var data = Encoding.UTF8.GetBytes(_ownedText);
            X11Native.XChangeProperty(_display, requestor, property, target, 8,
                X11Native.PropModeReplace, data, data.Length);
            return true;
        }

        if (target == _png && _ownedImage is not null)
        {
            X11Native.XChangeProperty(_display, requestor, property, target, 8,
                X11Native.PropModeReplace, _ownedImage, _ownedImage.Length);
            return true;
        }

        return false;
    }

    private bool OwnSelection()
    {
        X11Native.XSetSelectionOwner(_display, _clipboard, _window, X11Native.CurrentTime);
        X11Native.XFlush(_display);
        return X11Native.XGetSelectionOwner(_display, _clipboard) == _window;
    }

    // Converts on the pump thread and waits on the caller thread, so the pump
    // keeps running while the round trip completes. One read at a time; the
    // lock only guards the pending slot, never the wait.
    private byte[]? ReadTarget(string name)
    {
        var pending = new PendingRead();
        lock (_readLock)
        {
            _pending = pending;
        }

        Invoke<bool>(() =>
        {
            var target = X11Native.XInternAtom(_display, name, false);
            X11Native.XConvertSelection(
                _display, _clipboard, target, _property, _window, X11Native.CurrentTime);
            X11Native.XFlush(_display);
            return true;
        });

        if (!pending.Done.Task.Wait(ReadTimeoutMs))
        {
            lock (_readLock)
            {
                if (ReferenceEquals(_pending, pending)) _pending = null;
            }

            return null;
        }

        return pending.Bytes;
    }

    private async Task CaptureAsync(string? appName)
    {
        try
        {
            var targets = await Task.Run(() => ReadTargetNames());
            var pick = targets is null ? null : X11Selection.PickReadTarget(targets);
            if (pick == X11Selection.PngMime)
            {
                var png = await Task.Run(() => ReadImage());
                if (png is null) return;
                var thumbnail = await Task.Run(() => SkiaImageCodec.Thumbnail(png, ThumbnailMaxEdge)) ?? png;
                await _capture.CaptureImageAsync(
                    png, thumbnail, appName, DateTimeOffset.UtcNow,
                    _settings.Current.MaxItems, CancellationToken.None);
            }
            else if (pick is not null)
            {
                var text = await Task.Run(() => ReadText());
                if (text is null) return;
                await _capture.CaptureAsync(
                    text, appName, DateTimeOffset.UtcNow,
                    _settings.Current.MaxItems, CancellationToken.None);
            }

            await PruneByAgeAsync();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Capturing a clipboard change failed.");
        }
    }

    private string[]? ReadTargetNames()
    {
        var bytes = ReadTarget(X11Selection.Targets);
        if (bytes is null || bytes.Length % IntPtr.Size != 0 || bytes.Length == 0) return null;
        var count = bytes.Length / IntPtr.Size;
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            var atom = new IntPtr(BitConverter.ToInt64(bytes, i * IntPtr.Size));
            names[i] = Invoke(() => X11Native.ReadAtomName(_display, atom));
        }

        return names;
    }

    private async Task PruneByAgeAsync()
    {
        var days = _settings.Current.RetentionDays;
        if (days > 0)
            await _repository.PruneOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-days), CancellationToken.None);
    }

    private sealed class PendingRead
    {
        public TaskCompletionSource<bool> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public byte[]? Bytes { get; set; }
    }
}
