using B4B.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Firma> Firmalar => Set<Firma>();

    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Firma>(entity =>
        {
            entity.HasKey(firma => firma.Id);

            entity.Property(firma => firma.FirmaAdi)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(firma => firma.Domain)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(firma => firma.Domain)
                .IsUnique();

            entity.Property(firma => firma.ConfigDegeri)
                .HasMaxLength(250)
                .IsRequired();

            entity.HasData(
                new Firma
                {
                    Id = 1,
                    FirmaAdi = "Firma A",
                    Domain = "localhost:7101",
                    ConfigDegeri = "Firma A için örnek yapılandırma değeri"
                },
                new Firma
                {
                    Id = 2,
                    FirmaAdi = "Firma B",
                    Domain = "localhost:7201",
                    ConfigDegeri = "Firma B için örnek yapılandırma değeri"
                });
        });

        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasKey(kullanici => kullanici.Id);

            entity.Property(kullanici => kullanici.KullaniciAdi)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(kullanici => kullanici.SifreHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(kullanici => new { kullanici.FirmaId, kullanici.KullaniciAdi })
                .IsUnique();

            entity.HasOne(kullanici => kullanici.Firma)
                .WithMany(firma => firma.Kullanicilar)
                .HasForeignKey(kullanici => kullanici.FirmaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
