using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Features.ClipboardPanel;

// The per-row category menu: assign a category, take one away, or create one and assign it.
internal static class CategoryMenu
{
    public static async Task ShowAsync(
        FrameworkElement anchor,
        ClipItemViewModel item,
        ClipboardPanelViewModel panel,
        Windows.Foundation.Point? position = null)
    {
        var categories = await panel.GetCategoriesAsync();
        var flyout = new MenuFlyout();

        flyout.Items.Add(Choice(
            Tr.Get("Category_None"), item.CategoryId is null, () => panel.AssignCategoryAsync(item, null)));

        foreach (var category in categories)
            flyout.Items.Add(Choice(
                category.Name, item.CategoryId == category.Id, () => panel.AssignCategoryAsync(item, category.Id)));

        flyout.Items.Add(new MenuFlyoutSeparator());
        var create = new MenuFlyoutItem { Text = Tr.Get("Category_New") };
        create.Click += (_, _) => _ = CreateAndAssignAsync(anchor, item, panel);
        flyout.Items.Add(create);

        foreach (var entry in flyout.Items) TintHover(entry);

        if (position is { } point)
            flyout.ShowAt(anchor, new FlyoutShowOptions { Position = point });
        else
            flyout.ShowAt(anchor);
    }

    // Element-scoped hover washes for the menu options, as a second net next to the app-level keys.
    private static void TintHover(MenuFlyoutItemBase entry)
    {
        if (entry is MenuFlyoutSeparator) return;
        if (Application.Current.Resources["SystemAccentColor"] is not Windows.UI.Color accent) return;

        entry.Resources["MenuFlyoutItemBackgroundPointerOver"] = Wash(accent, 0.25);
        entry.Resources["ToggleMenuFlyoutItemBackgroundPointerOver"] = Wash(accent, 0.25);
        entry.Resources["MenuFlyoutItemBackgroundPressed"] = Wash(accent, 0.35);
        entry.Resources["ToggleMenuFlyoutItemBackgroundPressed"] = Wash(accent, 0.35);
    }

    private static SolidColorBrush Wash(Windows.UI.Color accent, double opacity) =>
        new(accent) { Opacity = opacity };

    private static ToggleMenuFlyoutItem Choice(string text, bool isChecked, Func<Task> assign)
    {
        var choice = new ToggleMenuFlyoutItem { Text = text, IsChecked = isChecked };
        choice.Click += (_, _) => _ = assign();
        return choice;
    }

    private static async Task CreateAndAssignAsync(
        FrameworkElement anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        var box = new TextBox { PlaceholderText = Tr.Get("Category_NewPlaceholder") };
        var dialog = new ContentDialog
        {
            XamlRoot = anchor.XamlRoot,
            Title = Tr.Get("Category_NewTitle"),
            Content = box,
            PrimaryButtonText = Tr.Get("Category_Create"),
            CloseButtonText = Tr.Get("Common_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        };
        box.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(box.Text);

        // Let the menu finish closing before the dialog opens over it.
        await Task.Yield();
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var category = await panel.GetOrAddCategoryAsync(box.Text);
        await panel.RefreshCategoryFilterAsync();
        await panel.AssignCategoryAsync(item, category.Id);
    }
}
