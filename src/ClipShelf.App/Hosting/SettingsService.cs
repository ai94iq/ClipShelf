namespace ClipShelf.App.Hosting;

// Holds the current settings in memory and persists every change, so services can read live values
// (maximum history size, paste behaviour) without reloading from disk.
public sealed class SettingsService(ISettingsStore store, AppSettings startup)
{
    public AppSettings Current { get; private set; } = startup;

    public event EventHandler? Changed;

    public void Update(AppSettings next)
    {
        Current = next;
        store.Save(next);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
