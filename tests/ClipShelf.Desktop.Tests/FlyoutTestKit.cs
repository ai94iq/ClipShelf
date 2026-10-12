using Avalonia;
using ClipShelf.Core.Abstractions;
using ClipShelf.Core.Models;
using ClipShelf.Desktop.Platform;
using ClipShelf.Desktop.Shell;
using ClipShelf.Presentation.Features.ClipboardPanel;
using ClipShelf.Presentation.Platform;
using ClipShelf.Presentation.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ClipShelf.Desktop.Tests;

// A flyout over a real view model with faked storage, clipboard and platform.
internal sealed class FlyoutTestKit
{
    public FakeFlyoutPlatform Platform { get; } = new();
    public IPasteBack PasteBack { get; } = Substitute.For<IPasteBack>();
    public IClipboardWriter Clipboard { get; } = Substitute.For<IClipboardWriter>();
    public IClipRepository Repository { get; } = Substitute.For<IClipRepository>();
    public ManualTime Time { get; } = new();
    public ClipboardPanelViewModel ViewModel { get; }
    public FlyoutWindow Window { get; }

    public FlyoutTestKit()
    {
        Repository.GetRecentAsync(default, default, default).ReturnsForAnyArgs(Array.Empty<ClipListItem>());
        ViewModel = new ClipboardPanelViewModel(
            Repository, Substitute.For<ICategoryRepository>(), Substitute.For<ICategoryLockService>(),
            Substitute.For<ILockPasswordService>(), Clipboard, Substitute.For<IDateFormatter>(),
            NullLogger<ClipboardPanelViewModel>.Instance);
        Window = new FlyoutWindow(ViewModel, Platform, PasteBack, Time);
    }

    public ClipItemViewModel AddClip(string text)
    {
        var item = new ClipItemViewModel(
            new ClipListItem(ViewModel.Items.Count + 1, text, "Notepad", false, DateTimeOffset.UtcNow, default!),
            Substitute.For<IDateFormatter>(), ViewModel);
        ViewModel.Items.Add(item);
        return item;
    }
}

internal sealed class FakeFlyoutPlatform : IFlyoutPlatform
{
    public IntPtr Foreground { get; set; } = new(42);
    public PixelPoint Pointer { get; set; } = new(100, 100);
    public IntPtr? RestoredTo { get; private set; }

    public IntPtr ForegroundWindow() => Foreground;

    public PixelPoint Cursor() => Pointer;

    public void ForceForeground(IntPtr window) { }

    public void RestoreForeground(IntPtr own, IntPtr previous) => RestoredTo = previous;
}

internal sealed class ManualTime : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
