namespace ClipShelf.App.Common;

// A labelled choice for a ComboBox: Value is bound, Label is shown.
public sealed record Option<T>(T Value, string Label);
