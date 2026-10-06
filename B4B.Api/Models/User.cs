namespace B4B.Api.Models;

public class User
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public Company? Company { get; set; }

    public Cart? Cart { get; set; }

    public List<Order> Orders { get; set; } = [];
}
