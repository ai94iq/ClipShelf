using System.Collections.Concurrent;
using System.Runtime.Versioning;
using ClipShelf.Core.Input;

namespace ClipShelf.Desktop.Platform.Linux;

// One global shortcut on the root window, mirroring the Windows message-window
// hotkey. A pump thread owns the display; registration marshals onto it. Grab
// conflicts surface synchronously through a scoped error handler (saved and
// restored around the grab), so a taken shortcut reports failure instead of
// taking the process down. Without an X display the pump exits and
// registration reports failure, which is exactly the settings warning path.
[SupportedOSPlatform("linux")]
public sealed class X11HotkeyService : IGlobalHotkey, IDisposable
{
    private const int GrabModeAsync = 1;
    private const int PumpPollMs = 20;
    private const int CallTimeoutMs = 5000;
    private const uint LockBits = X11Hotkey.LockMask | X11Hotkey.Mod2Mask;

    private readonly ILogger<X11HotkeyService> _log;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _pump;
    private readonly X11Native.XErrorHandler _errorHandler;
    private IntPtr _display;
    private IntPtr _root;
    private int _keycode;
    private uint _mask;
    private bool _hasGrab;
    private bool _grabFailed;
    private bool _disposed;

    public X11HotkeyService(ILogger<X11HotkeyService> log)
    {
        _log = log;
        _errorHandler = OnXError;
        _pump = new Thread(Pump) { IsBackground = true, Name = "X11Hotkey" };
        if (OperatingSystem.IsLinux()) _pump.Start();
    }

    public event EventHandler? Pressed;

    public bool Register(HotkeyGesture gesture)
    {
        var name = X11Hotkey.ToKeysymName(gesture.VirtualKey);
        if (name is null)
        {
            _log.LogWarning("Hotkey key has no X11 equivalent.");
            return false;
        }

        try
        {
            return Call(() => Grab(name, X11Hotkey.ToMask(gesture.Modifiers)));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Registering the global hotkey failed.");
            return false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        if (OperatingSystem.IsLinux() && _pump.IsAlive) _pump.Join(TimeSpan.FromSeconds(2));
        FinishQueue();
        _queue.Dispose();
    }

    private T Call<T>(Func<T> work, int timeoutMs = CallTimeoutMs)
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
            throw new InvalidOperationException("The X11 hotkey pump is not running.");
        }

        if (!done.Task.Wait(timeoutMs))
            throw new TimeoutException("The X11 hotkey pump did not answer in time.");
        return done.Task.GetAwaiter().GetResult();
    }

    private void Pump()
    {
        try
        {
            _display = X11Native.XOpenDisplay(null);
            if (_display == IntPtr.Zero)
                throw new InvalidOperationException("XOpenDisplay returned no display.");
            _root = X11Native.XDefaultRootWindow(_display);
        }
        catch (Exception ex) when (ex is DllNotFoundException || ex is InvalidOperationException)
        {
            _log.LogWarning(ex, "No X display; the global hotkey is unavailable.");
            _queue.CompleteAdding();
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
            _log.LogError(ex, "The X11 hotkey pump failed.");
        }
        finally
        {
            FinishQueue();
            if (_display != IntPtr.Zero) X11Native.XCloseDisplay(_display);
        }
    }

    // CompleteAdding throws when already completed; both the dispose path and
    // the pump exit race to finish the queue.
    private void FinishQueue()
    {
        try
        {
            _queue.CompleteAdding();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private bool Grab(string keysymName, uint mask)
    {
        Release();
        var keysym = X11Native.XStringToKeysym(keysymName);
        if (keysym == 0) return false;
        var keycode = X11Native.XKeysymToKeycode(_display, keysym);
        if (keycode == 0) return false;

        // Grab errors arrive asynchronously; flush under a scoped handler so a
        // taken shortcut fails this call instead of killing the process. The
        // pump runs nothing else while this lambda executes.
        _grabFailed = false;
        var previous = X11Native.XSetErrorHandler(_errorHandler);
        try
        {
            foreach (var combo in X11Hotkey.GrabMasks(mask))
                X11Native.XGrabKey(_display, (int)keycode, combo, _root, false, GrabModeAsync, GrabModeAsync);
            X11Native.XSync(_display, false);
        }
        finally
        {
            X11Native.XSetErrorHandler(previous);
        }

        if (_grabFailed) return false;
        _keycode = (int)keycode;
        _mask = mask;
        _hasGrab = true;
        return true;
    }

    private void Release()
    {
        if (!_hasGrab) return;
        _hasGrab = false;
        foreach (var combo in X11Hotkey.GrabMasks(_mask))
            X11Native.XUngrabKey(_display, _keycode, combo, _root);
    }

    private void OnEvent(byte[] evt)
    {
        if (X11Selection.TypeOf(evt) != X11Hotkey.KeyPress) return;
        if (!_hasGrab) return;
        var state = BitConverter.ToUInt32(evt, 80) & ~LockBits;
        var keycode = BitConverter.ToUInt32(evt, 84);
        if (keycode == (uint)_keycode && state == _mask)
            Pressed?.Invoke(this, EventArgs.Empty);
    }

    private int OnXError(ref X11Native.XErrorEvent error)
    {
        if (error.ErrorCode == X11Hotkey.BadAccess && error.RequestCode == X11Hotkey.GrabKeyRequest)
            _grabFailed = true;
        return 0;
    }
}
