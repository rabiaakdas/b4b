using B4B.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Data;

public static class SeedData
{
    public static async Task EnsureUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = new PasswordHasher<Kullanici>();

        var firmaA = await dbContext.Firmalar.FirstAsync(firma => firma.Domain == "localhost:7101");
        var firmaB = await dbContext.Firmalar.FirstAsync(firma => firma.Domain == "localhost:7201");

        await EnsureUserAsync(dbContext, passwordHasher, firmaA.Id, "demo", "FirmaA123!");
        await EnsureUserAsync(dbContext, passwordHasher, firmaB.Id, "demo", "FirmaB123!");

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        AppDbContext dbContext,
        PasswordHasher<Kullanici> passwordHasher,
        int firmaId,
        string kullaniciAdi,
        string sifre)
    {
        var exists = await dbContext.Kullanicilar
            .AnyAsync(kullanici => kullanici.FirmaId == firmaId && kullanici.KullaniciAdi == kullaniciAdi);

        if (exists)
        {
            return;
        }

        var kullanici = new Kullanici
        {
            FirmaId = firmaId,
            KullaniciAdi = kullaniciAdi
        };

        kullanici.SifreHash = passwordHasher.HashPassword(kullanici, sifre);
        dbContext.Kullanicilar.Add(kullanici);
    }
}
