namespace ClipShelf.App.Hosting;

// Lets any service take Lazy<T> in its constructor; T is resolved on first .Value.
public sealed class LazyService<T>(IServiceProvider services)
    : Lazy<T>(services.GetRequiredService<T>) where T : notnull;
