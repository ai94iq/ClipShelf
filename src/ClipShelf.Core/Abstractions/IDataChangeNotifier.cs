namespace ClipShelf.Core.Abstractions;

public interface IDataChangeNotifier
{
    void Notify(DataChanged change);
}
