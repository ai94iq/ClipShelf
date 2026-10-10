using Avalonia.Controls;
using ClipShelf.Desktop.Views;

namespace ClipShelf.Desktop.Features;

// The per-row category menu: assign a category, take one away, or create one and assign it.
internal static class CategoryMenu
{
    public static async Task ShowAsync(Control anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        var categories = await panel.GetCategoriesAsync();
        var flyout = new MenuFlyout();

        flyout.Items.Add(Choice(
            Tr.Get("Category_None"), item.CategoryId is null, () => panel.AssignCategoryAsync(item, null)));

        foreach (var category in categories)
            flyout.Items.Add(Choice(
                category.Name, item.CategoryId == category.Id, () => panel.AssignCategoryAsync(item, category.Id)));

        flyout.Items.Add(new Separator());
        var create = new MenuItem { Header = Tr.Get("Category_New") };
        create.Click += (_, _) => _ = CreateAndAssignAsync(anchor, item, panel);
        flyout.Items.Add(create);

        // At the pointer, where the user right-clicked the cell.
        flyout.ShowAt(anchor, true);
    }

    private static MenuItem Choice(string text, bool isChecked, Func<Task> assign)
    {
        var choice = new MenuItem
        {
            Header = text,
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = isChecked,
        };
        choice.Click += (_, _) => _ = assign();
        return choice;
    }

    private static async Task CreateAndAssignAsync(
        Control anchor, ClipItemViewModel item, ClipboardPanelViewModel panel)
    {
        if (TopLevel.GetTopLevel(anchor) is not Window owner) return;

        var name = await NewCategoryDialog.ShowAsync(owner);
        if (string.IsNullOrWhiteSpace(name)) return;

        var category = await panel.GetOrAddCategoryAsync(name);
        await panel.RefreshCategoryFilterAsync();
        await panel.AssignCategoryAsync(item, category.Id);
    }
}
