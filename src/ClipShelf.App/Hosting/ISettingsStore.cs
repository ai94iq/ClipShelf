namespace ClipShelf.App.Hosting;

// Where the app settings live, so services can be tested without touching the disk.
public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
