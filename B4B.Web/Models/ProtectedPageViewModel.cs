namespace B4B.Web.Models;

public class ProtectedPageViewModel
{
    public Guid UserId { get; set; }

    public Guid CompanyId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;
}
