using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using B4B.Web.Models;
using B4B.Web.Services;

namespace B4B.Web.Controllers;

public class HomeController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ApiConfigClient _apiConfigClient;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IConfiguration configuration,
        ApiConfigClient apiConfigClient,
        ILogger<HomeController> logger)
    {
        _configuration = configuration;
        _apiConfigClient = apiConfigClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var clientHost = (HttpContext.Request.Host.Value ?? string.Empty).ToLowerInvariant();
        var allowedHosts = _configuration
            .GetSection("AllowedClientHosts")
            .Get<string[]>() ?? [];

        if (!allowedHosts.Contains(clientHost, StringComparer.OrdinalIgnoreCase))
        {
            return View(new CompanyConfigViewModel
            {
                ClientHost = clientHost,
                Success = false,
                ErrorMessage = $"Bu MVC adresi için izin yok: {clientHost}"
            });
        }

        try
        {
            var result = await _apiConfigClient.GetConfigAsync(clientHost);

            return View(new CompanyConfigViewModel
            {
                ClientHost = clientHost,
                Success = result.Success,
                CompanyId = result.Config?.Id,
                CompanyName = result.Config?.CompanyName,
                ConfigValue = result.Config?.ConfigValue,
                ErrorMessage = result.ErrorMessage
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API yapılandırma isteği başarısız oldu.");

            return View(new CompanyConfigViewModel
            {
                ClientHost = clientHost,
                Success = false,
                ErrorMessage = "API'ye ulaşılamadı. API uygulamasının https://localhost:7001 adresinde çalıştığından emin olun."
            });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
