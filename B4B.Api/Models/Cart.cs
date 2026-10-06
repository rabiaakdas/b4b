namespace B4B.Api.Models;

public class Cart
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid UserId { get; set; }

    public Company? Company { get; set; }

    public User? User { get; set; }

    public List<CartItem> Items { get; set; } = [];
}
