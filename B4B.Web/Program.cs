using Microsoft.AspNetCore.Authentication.Cookies;
using B4B.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var cookieSuffix = builder.Configuration["CookieSuffix"] ?? "Default";

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ApiConfigClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(apiBaseUrl ?? "https://localhost:7001");
});
builder.Services.AddHttpClient<ApiAuthClient>(client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(apiBaseUrl ?? "https://localhost:7001");
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = $".B4B.Web.{cookieSuffix}.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = $".B4B.Web.{cookieSuffix}.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = $".B4B.Web.{cookieSuffix}.Auth";
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
