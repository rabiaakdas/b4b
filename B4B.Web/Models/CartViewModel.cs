namespace B4B.Web.Models;

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = [];

    public decimal GrandTotal { get; set; }

    public string? ErrorMessage { get; set; }
}

public class CartItemViewModel
{
    public Guid Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
