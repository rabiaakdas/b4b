namespace B4B.Web.Models;

public class OrdersViewModel
{
    public List<OrderSummaryViewModel> Orders { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class OrderSummaryViewModel
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }
}

public class OrderDetailViewModel
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemDetailViewModel> Items { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class OrderItemDetailViewModel
{
    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
