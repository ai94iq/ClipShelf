using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Copyloft.Core.Models;
using Copyloft.Desktop.Features;
using NSubstitute;

namespace Copyloft.Desktop.Tests;

public sealed class CategoryMenuTests
{
    private static async Task<(ViewModelKit Kit, Window Window, MenuFlyout Menu)> OpenMenu()
    {
        var kit = new ViewModelKit();
        kit.StoreClip("one");
        kit.StoredCategories.Add(new Category(4, "Work"));
        var page = new HistoryPage(kit.ViewModel);
        var window = new Window { Content = page, Width = 800, Height = 600 };
        window.Show();
        await page.LoadAsync();
        Dispatcher.UIThread.RunJobs();

        var button = page.GetVisualDescendants().OfType<Button>().First(b => b.Classes.Contains("category"));
        button.Command!.Execute(button.CommandParameter);   // what a click runs
        for (var i = 0; i < 3; i++) { await Task.Yield(); Dispatcher.UIThread.RunJobs(); }

        var menu = Assert.IsType<MenuFlyout>(FlyoutBase.GetAttachedFlyout(button));
        return (kit, window, menu);
    }

    private static List<MenuItem> Items(MenuFlyout menu) => menu.Items.OfType<MenuItem>().ToList();

    [Fact]
    public Task The_row_menu_lists_none_each_category_and_new() => Ui.Run(async () =>
    {
        var (_, _, menu) = await OpenMenu();

        var items = Items(menu);
        Assert.Equal(["No category", "Work", "New category…"], items.Select(i => i.Header as string));
        Assert.True(items[0].IsChecked);
        Assert.False(items[1].IsChecked);
    });

    [Fact]
    public Task Picking_a_category_assigns_it() => Ui.Run(async () =>
    {
        var (kit, _, menu) = await OpenMenu();

        Items(menu)[1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        await kit.Repository.Received().AssignCategoryAsync(1, 4, Arg.Any<CancellationToken>());
    });

    [Fact]
    public Task New_category_creates_and_assigns_it() => Ui.Run(async () =>
    {
        var (kit, window, menu) = await OpenMenu();
        kit.Categories.GetOrAddAsync("Recipes", Arg.Any<CancellationToken>()).Returns(new Category(9, "Recipes"));

        Items(menu)[^1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        for (var i = 0; i < 3; i++) { await Task.Yield(); Dispatcher.UIThread.RunJobs(); }
        var dialog = DialogDriver.Open(window);
        var create = dialog.FindControl<Button>("PrimaryButton")!;
        Assert.False(create.IsEnabled);
        DialogDriver.Boxes(dialog)[0].Text = "Recipes";
        DialogDriver.Click(create);
        for (var i = 0; i < 3; i++) { await Task.Yield(); Dispatcher.UIThread.RunJobs(); }

        await kit.Repository.Received().AssignCategoryAsync(1, 9, Arg.Any<CancellationToken>());
    });
}
