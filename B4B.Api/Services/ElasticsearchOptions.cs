namespace B4B.Api.Services;

public class ElasticsearchOptions
{
    public string Url { get; set; } = "http://localhost:9200";

    public string ProductIndex { get; set; } = "b4b-products";

    public int ProductSearchSize { get; set; } = 100;
}
