using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ClipShelf.Desktop.Controls;

// Avalonia's ListBox has no grouping, so the groups (Pinned, Recent) are flattened into one list:
// each group's header (the ClipGroup itself) followed by its clips, kept in step with every change.
// ponytail: rebuilt whole on each change; fine at page sizes, diff it if long histories stutter.
public sealed class GroupedRows : ObservableCollection<object>
{
    private readonly ObservableCollection<ClipGroup> _groups;

    public GroupedRows(ObservableCollection<ClipGroup> groups)
    {
        _groups = groups;
        _groups.CollectionChanged += OnGroupsChanged;
        foreach (var group in _groups) group.CollectionChanged += OnGroupChanged;
        Rebuild();
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (ClipGroup group in e.OldItems ?? Array.Empty<ClipGroup>()) group.CollectionChanged -= OnGroupChanged;
        foreach (ClipGroup group in e.NewItems ?? Array.Empty<ClipGroup>()) group.CollectionChanged += OnGroupChanged;
        Rebuild();
    }

    private void OnGroupChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        var rows = _groups.SelectMany(group => group.Count == 0 ? [] : group.Prepend<object>(group)).ToList();
        if (rows.SequenceEqual(this)) return;

        Clear();
        foreach (var row in rows) Add(row);
    }
}
