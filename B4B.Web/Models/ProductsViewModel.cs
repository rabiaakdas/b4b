namespace B4B.Web.Models;

public class ProductsViewModel
{
    public string? Arama { get; set; }

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
