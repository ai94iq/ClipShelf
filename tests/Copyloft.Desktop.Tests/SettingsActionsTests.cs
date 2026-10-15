using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Copyloft.Core.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Copyloft.Desktop.Tests;

public sealed class SettingsActionsTests
{
    private static Window Host(Control page)
    {
        var window = new Window { Content = page, Width = 900, Height = 700 };
        window.Show();
        return window;
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [Fact]
    public Task A_finished_export_says_how_many_clips() => Ui.Run(async () =>
    {
        var kit = new SettingsKit();
        kit.Export.ExportAsync(default!, default, default).ReturnsForAnyArgs(3);
        Host(kit.Page);

        await kit.ViewModel.ExportAsync("out.json");

        Assert.True(kit.Page.FindControl<Control>("ExportStatus")!.IsVisible);
        Assert.False(kit.Page.FindControl<Control>("ExportError")!.IsVisible);
    });

    [Fact]
    public Task A_failed_export_shows_the_error_line() => Ui.Run(async () =>
    {
        var kit = new SettingsKit();
        kit.Export.ExportAsync(default!, default, default).ThrowsAsyncForAnyArgs(new IOException("disk full"));
        Host(kit.Page);

        await kit.ViewModel.ExportAsync("out.json");

        Assert.True(kit.Page.FindControl<Control>("ExportError")!.IsVisible);
        Assert.False(kit.Page.FindControl<Control>("ExportStatus")!.IsVisible);
    });

    [Fact]
    public Task Check_for_updates_asks_and_offers_the_download() => Ui.Run(async () =>
    {
        var kit = new SettingsKit();
        kit.Updates.NewerVersionAsync(default).ReturnsForAnyArgs("9.9.9");
        Host(kit.Page);
        Assert.False(kit.Page.FindControl<Control>("DownloadLink")!.IsVisible);

        Click(kit.Page.FindControl<Button>("CheckUpdatesButton")!);
        await Task.Yield();
        Dispatcher.UIThread.RunJobs();

        await kit.Updates.ReceivedWithAnyArgs().NewerVersionAsync(default);
        Assert.True(kit.Page.FindControl<Control>("DownloadLink")!.IsVisible);
        Assert.True(kit.Page.FindControl<Control>("UpdateStatus")!.IsVisible);
    });

    [Fact]
    public Task Lock_now_hides_an_unlocked_category_again() => Ui.Run(async () =>
    {
        var kit = new SettingsKit();
        kit.StoredCategories.Add(new Category(7, "Work", IsLocked: true));
        kit.Locks.UnlockedCategoryIds.Returns([7L]);
        Host(kit.Page);
        await kit.Page.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        var lockNow = kit.Page.FindControl<ItemsControl>("CategoryList")!.GetVisualDescendants()
            .OfType<Button>().Single(b => b.Classes.Contains("locknow"));
        Assert.True(lockNow.IsVisible);
        Click(lockNow);
        Dispatcher.UIThread.RunJobs();

        kit.Locks.Received().Lock(7);
        await kit.Categories.Received().SetLockedAsync(7, true, Arg.Any<CancellationToken>());
    });

    [Fact]
    public Task Exit_asks_the_shell_to_exit() => Ui.Run(() =>
    {
        var kit = new SettingsKit();
        var exited = false;
        kit.Shell.ExitRequested += (_, _) => exited = true;
        Host(kit.Page);

        Click(kit.Page.FindControl<Button>("ExitButton")!);

        Assert.True(exited);
    });
}
