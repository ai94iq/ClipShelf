namespace ClipShelf.Desktop.Controls;

// UI events start work they cannot await. A failure (a locked database, a picker error) is logged
// instead of escaping on the dispatcher and ending the process.
internal static class UiTask
{
    public static async void Run(Func<Task> work, string what)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "{What} failed", what);
        }
    }
}
