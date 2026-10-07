using System.Text.Json.Serialization;

namespace B4B.Api.Services;

public class ProductSearchDocument
{
    [JsonPropertyName("ProductId")]
    public Guid ProductId { get; set; }

    [JsonPropertyName("CompanyId")]
    public Guid CompanyId { get; set; }

    [JsonPropertyName("ProductCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("Price")]
    public decimal Price { get; set; }
}
