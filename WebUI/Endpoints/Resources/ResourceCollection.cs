namespace WebUI.Endpoints.Resources;

public class ResourceCollection<T>
{
    public ResourceCollection(IEnumerable<T> items)
    {
        Items = items is IList<T> itemsList ? itemsList : items.ToList();
        Amount = Items.Count;
        Total = Items.Count;
    }

    public IList<T> Items { get; } = Array.Empty<T>();
    public long Total { get; init; }
    public long Amount { get; }
}
