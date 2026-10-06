using System.ComponentModel.DataAnnotations;

namespace B4B.Web.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Firma kodu zorunludur.")]
    [Display(Name = "Firma Kodu")]
    public string CompanyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [Display(Name = "Şifre")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class AdminPanelViewModel
{
    public string CompanyName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
}

public class AdminUsersViewModel
{
    public List<AdminUserViewModel> Users { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class AdminUserViewModel
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
}

public class AdminOrdersViewModel
{
    public List<AdminOrderSummaryViewModel> Orders { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class AdminOrderSummaryViewModel
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }
}

public class AdminOrderDetailViewModel
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public List<AdminOrderItemViewModel> Items { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class AdminOrderItemViewModel
{
    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
