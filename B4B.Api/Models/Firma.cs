namespace B4B.Api.Models;

public class Firma
{
    public int Id { get; set; }

    public string FirmaAdi { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string ConfigDegeri { get; set; } = string.Empty;

    public List<Kullanici> Kullanicilar { get; set; } = [];
}
