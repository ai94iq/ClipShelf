namespace Copyloft.Core.Abstractions;

public interface IDataChangeNotifier
{
    void Notify(DataChanged change);
}
