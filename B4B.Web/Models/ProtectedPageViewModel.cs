namespace B4B.Web.Models;

public class ProtectedPageViewModel
{
    public int UserId { get; set; }

    public int FirmaId { get; set; }

    public string KullaniciAdi { get; set; } = string.Empty;

    public string FirmaAdi { get; set; } = string.Empty;
}
