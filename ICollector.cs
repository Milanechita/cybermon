namespace CyberMon;

public interface ICollector<T>
{
    Task<T> CollectAsync(CancellationToken ct);
}
