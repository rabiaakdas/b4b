namespace B4B.Api.Models;

public class Kullanici
{
    public int Id { get; set; }

    public int FirmaId { get; set; }

    public string KullaniciAdi { get; set; } = string.Empty;

    public string SifreHash { get; set; } = string.Empty;

    public Firma? Firma { get; set; }
}
