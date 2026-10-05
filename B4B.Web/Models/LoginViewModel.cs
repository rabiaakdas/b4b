using System.ComponentModel.DataAnnotations;

namespace B4B.Web.Models;

public class LoginViewModel
{
    public string ClientHost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [Display(Name = "Kullanıcı Adı")]
    public string KullaniciAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [Display(Name = "Şifre")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;

    public string? HataMesaji { get; set; }
}
