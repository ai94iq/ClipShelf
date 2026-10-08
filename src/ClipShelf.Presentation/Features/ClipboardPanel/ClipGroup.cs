using System.Collections.ObjectModel;

namespace ClipShelf.Presentation.Features.ClipboardPanel;

// A titled group of clips (Pinned, Recent) for the history page.
public sealed class ClipGroup(string title) : ObservableCollection<ClipItemViewModel>
{
    public string Title { get; } = title;
}
