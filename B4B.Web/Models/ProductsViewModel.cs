namespace B4B.Web.Models;

public class ProductsViewModel
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public long TotalCount { get; set; }

    public long ElasticsearchTookMilliseconds { get; set; }

    public long SearchDurationMilliseconds { get; set; }

    public string? CacheStatus { get; set; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page * PageSize < TotalCount;

    public List<ProductViewModel> Products { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class ProductViewModel
{
    public Guid Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }
}
