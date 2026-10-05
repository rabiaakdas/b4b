namespace B4B.Web.Models;

public class FirmaConfigViewModel
{
    public string ClientHost { get; set; } = string.Empty;

    public bool Basarili { get; set; }

    public int? FirmaId { get; set; }

    public string? FirmaAdi { get; set; }

    public string? ConfigDegeri { get; set; }

    public string? HataMesaji { get; set; }
}
