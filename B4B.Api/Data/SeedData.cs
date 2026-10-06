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
        var passwordHasher = new PasswordHasher<User>();

        var companyA = await dbContext.Companies.FirstAsync(company => company.Domain == "localhost:7101");
        var companyB = await dbContext.Companies.FirstAsync(company => company.Domain == "localhost:7201");

        await EnsureUserAsync(dbContext, passwordHasher, companyA.Id, "demo", "FirmaA123!", isAdmin: false);
        await EnsureUserAsync(dbContext, passwordHasher, companyB.Id, "demo", "FirmaB123!", isAdmin: false);
        await EnsureUserAsync(dbContext, passwordHasher, companyA.Id, "admin", "AdminA123!", isAdmin: true);
        await EnsureUserAsync(dbContext, passwordHasher, companyB.Id, "admin", "AdminB123!", isAdmin: true);

        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        AppDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        Guid companyId,
        string username,
        string password,
        bool isAdmin)
    {
        var exists = await dbContext.Users
            .AnyAsync(user => user.CompanyId == companyId && user.Username == username);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            CompanyId = companyId,
            Username = username,
            IsAdmin = isAdmin
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        dbContext.Users.Add(user);
    }
}
