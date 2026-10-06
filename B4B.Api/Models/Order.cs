namespace B4B.Api.Models;

public class Order
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public Company? Company { get; set; }

    public User? User { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}
