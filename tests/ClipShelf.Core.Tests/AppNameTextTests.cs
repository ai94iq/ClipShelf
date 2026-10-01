using ClipShelf.Core.Text;

namespace ClipShelf.Core.Tests;

public sealed class AppNameTextTests
{
    [Theory]
    [InlineData("chrome", "Chrome")]
    [InlineData("msedge", "Edge")]
    [InlineData("winword", "Word")]
    [InlineData("WINWORD", "Word")]
    [InlineData("notepad.EXE", "Notepad")]
    [InlineData("code", "VS Code")]
    [InlineData("windowsterminal", "Windows Terminal")]
    [InlineData("vlc", "VLC")]
    public void Known_process_names_map_to_readable_names(string processName, string expected) =>
        Assert.Equal(expected, AppNameText.Display(processName));

    [Theory]
    [InlineData("myapp", "Myapp")]
    [InlineData("FOOBAR", "Foobar")]
    [InlineData("SomeApp", "SomeApp")]
    public void Other_process_names_are_capitalized(string processName, string expected) =>
        Assert.Equal(expected, AppNameText.Display(processName));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_process_name_stays_empty(string? processName) =>
        Assert.Null(AppNameText.Display(processName));
}
