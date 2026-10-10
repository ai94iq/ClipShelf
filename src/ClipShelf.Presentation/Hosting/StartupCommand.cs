namespace ClipShelf.Presentation.Hosting;

// The launch commands a second instance can send to the running app.
public static class StartupCommand
{
    public const string Toggle = "--toggle";

    public const string History = "--history";

    public const string Quit = "--quit";

    // The first known command in the arguments, or null.
    public static string? Parse(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg == Toggle) return Toggle;
            if (arg == History) return History;
            if (arg == Quit) return Quit;
        }

        return null;
    }
}
