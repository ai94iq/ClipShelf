namespace Copyloft.Presentation.Platform;

// Protects the database key at rest: DPAPI on Windows, the system keyring elsewhere.
public interface IKeyStore
{
    string LoadOrCreate(string path);
}
