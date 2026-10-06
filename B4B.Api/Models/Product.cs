namespace B4B.Api.Models;

public class Product
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Company? Company { get; set; }

    public List<CartItem> CartItems { get; set; } = [];

    public List<OrderItem> OrderItems { get; set; } = [];
}
