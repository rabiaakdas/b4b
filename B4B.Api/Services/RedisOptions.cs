namespace B4B.Api.Services;

public class RedisOptions
{
    public string Configuration { get; set; } = "localhost:6379";

    public int ProductSearchCacheSeconds { get; set; } = 60;
}
