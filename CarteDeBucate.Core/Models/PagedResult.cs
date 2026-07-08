public class PagedResult<T>
{
    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalItems)
    {
        Items = items?.ToList() ?? new List<T>();
        PageNumber = Math.Max(1, pageNumber);
        PageSize = Math.Max(1, pageSize);
        TotalItems = Math.Max(0, totalItems);
        TotalPages = TotalItems == 0
            ? 0
            : (int)Math.Ceiling((double)TotalItems / PageSize);
    }

    public List<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1 && TotalPages > 0;
    public bool HasNextPage => PageNumber < TotalPages;
}
