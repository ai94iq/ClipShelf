namespace ClipShelf.Presentation.Services;

// The one password that guards exports and locked categories.
public interface ILockPasswordService
{
    bool IsSet { get; }

    bool Verify(string password);

    void Set(string password);

    void Remove();
}
