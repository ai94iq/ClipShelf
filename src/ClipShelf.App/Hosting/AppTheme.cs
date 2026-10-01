namespace ClipShelf.App.Hosting;

public enum AppTheme
{
    System,

    // Full white surfaces (the app's light theme).
    Light,

    Dark,

    // Full black surfaces.
    Black,

    // Alias kept so settings saved while it was offered still load; behaves like Light.
    White,
}
