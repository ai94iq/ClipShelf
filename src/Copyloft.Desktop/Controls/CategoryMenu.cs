using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Copyloft.Desktop.Controls;

// The per-row category menu: assign a category, take one away, or create one and assign it.
internal static class CategoryMenu
{
    // Fire-and-forget from the row button, so a failure is logged here instead of lost.
    public static async Task ShowAsync(Control anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        try
        {
            await ShowCoreAsync(anchor, item, panel);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Showing the category menu failed");
        }
    }

    private static async Task ShowCoreAsync(Control anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        var categories = await panel.GetCategoriesAsync();
        var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };

        menu.Items.Add(Choice(Tr.Get("Category_None"), item.CategoryId is null, () => panel.AssignCategoryAsync(item, null)));
        foreach (var category in categories)
            menu.Items.Add(Choice(category.Name, item.CategoryId == category.Id, () => panel.AssignCategoryAsync(item, category.Id)));

        menu.Items.Add(new Separator());
        var create = new MenuItem { Header = Tr.Get("Category_New") };
        create.Click += (_, _) => _ = CreateAndAssignAsync(anchor, item, panel);
        menu.Items.Add(create);

        FlyoutBase.SetAttachedFlyout(anchor, menu);
        FlyoutBase.ShowAttachedFlyout(anchor);
    }

    // Lists the menu for a row button, or the list itself when the row is not realized.
    public static Control Anchor(ListBox list, ClipItemViewModel item) =>
        list.ContainerFromItem(item) is Control row
            ? row.GetVisualDescendantsOfType<Button>().FirstOrDefault(b => b.Classes.Contains("category")) ?? row
            : list;

    private static MenuItem Choice(string text, bool isChecked, Func<Task> assign)
    {
        var choice = new MenuItem { Header = text, ToggleType = MenuItemToggleType.CheckBox, IsChecked = isChecked };
        choice.Click += (_, _) => _ = assign();
        return choice;
    }

    private static async Task CreateAndAssignAsync(Control anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        var box = new TextBox
        {
            PlaceholderText = Tr.Get("Category_NewPlaceholder"),
            [Avalonia.Automation.AutomationProperties.NameProperty] = Tr.Get("Category_NewPlaceholder"),
        };
        var dialog = new InlineDialog(Tr.Get("Category_NewTitle"), Tr.Get("Category_Create"), box) { IsPrimaryEnabled = false };
        box.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) dialog.IsPrimaryEnabled = !string.IsNullOrWhiteSpace(box.Text);
        };

        if (!await dialog.ShowAsync(anchor)) return;

        var category = await panel.GetOrAddCategoryAsync(box.Text ?? string.Empty);
        await panel.RefreshCategoryFilterAsync();
        await panel.AssignCategoryAsync(item, category.Id);
    }

    private static IEnumerable<T> GetVisualDescendantsOfType<T>(this Control control) =>
        Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(control).OfType<T>();
}
