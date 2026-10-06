namespace B4B.Api.Models;

public class Company
{
    public Guid Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string CompanyCode { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string ConfigValue { get; set; } = string.Empty;

    public List<User> Users { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<Cart> Carts { get; set; } = [];

    public List<Order> Orders { get; set; } = [];
}
