namespace B4B.Web.Models;

public class CompanyConfigViewModel
{
    public string ClientHost { get; set; } = string.Empty;

    public bool Success { get; set; }

    public Guid? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public string? ConfigValue { get; set; }

    public string? ErrorMessage { get; set; }
}
