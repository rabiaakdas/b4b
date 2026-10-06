using B4B.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace B4B.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Firmalar");
            entity.HasKey(company => company.Id);

            entity.Property(company => company.CompanyName)
                .HasColumnName("FirmaAdi")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(company => company.CompanyCode)
                .HasColumnName("FirmaKodu")
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(company => company.CompanyCode)
                .HasDatabaseName("IX_Firmalar_FirmaKodu")
                .IsUnique();

            entity.Property(company => company.Domain)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(company => company.Domain)
                .HasDatabaseName("IX_Firmalar_Domain")
                .IsUnique();

            entity.Property(company => company.ConfigValue)
                .HasColumnName("ConfigDegeri")
                .HasMaxLength(250)
                .IsRequired();

            entity.HasData(
                new Company
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    CompanyName = "Firma A",
                    CompanyCode = "FIRMAA",
                    Domain = "localhost:7101",
                    ConfigValue = "Firma A için örnek yapılandırma değeri"
                },
                new Company
                {
                    Id = new Guid("22222222-2222-2222-2222-222222222222"),
                    CompanyName = "Firma B",
                    CompanyCode = "FIRMAB",
                    Domain = "localhost:7201",
                    ConfigValue = "Firma B için örnek yapılandırma değeri"
                });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Kullanicilar");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.CompanyId)
                .HasColumnName("FirmaId");

            entity.Property(user => user.Username)
                .HasColumnName("KullaniciAdi")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasColumnName("SifreHash")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(user => user.IsAdmin)
                .HasColumnName("AdminMi")
                .IsRequired();

            entity.HasIndex(user => new { user.CompanyId, user.Username })
                .HasDatabaseName("IX_Kullanicilar_FirmaId_KullaniciAdi")
                .IsUnique();

            entity.HasOne(user => user.Company)
                .WithMany(company => company.Users)
                .HasForeignKey(user => user.CompanyId)
                .HasConstraintName("FK_Kullanicilar_Firmalar_FirmaId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Urunler");
            entity.HasKey(product => product.Id);

            entity.Property(product => product.CompanyId)
                .HasColumnName("FirmaId");

            entity.Property(product => product.ProductCode)
                .HasColumnName("UrunKodu")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(product => product.ProductName)
                .HasColumnName("UrunAdi")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(product => product.Price)
                .HasColumnName("Fiyat")
                .HasPrecision(18, 2);

            entity.HasIndex(product => new { product.CompanyId, product.ProductCode })
                .HasDatabaseName("IX_Urunler_FirmaId_UrunKodu")
                .IsUnique();

            entity.HasOne(product => product.Company)
                .WithMany(company => company.Products)
                .HasForeignKey(product => product.CompanyId)
                .HasConstraintName("FK_Urunler_Firmalar_FirmaId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new Product
                {
                    Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                    CompanyId = new Guid("11111111-1111-1111-1111-111111111111"),
                    ProductCode = "A-100",
                    ProductName = "Firma A Laptop",
                    Price = 25000m
                },
                new Product
                {
                    Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                    CompanyId = new Guid("11111111-1111-1111-1111-111111111111"),
                    ProductCode = "A-200",
                    ProductName = "Firma A Klavye",
                    Price = 1250m
                },
                new Product
                {
                    Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                    CompanyId = new Guid("22222222-2222-2222-2222-222222222222"),
                    ProductCode = "B-100",
                    ProductName = "Firma B Monitör",
                    Price = 8500m
                },
                new Product
                {
                    Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
                    CompanyId = new Guid("22222222-2222-2222-2222-222222222222"),
                    ProductCode = "B-200",
                    ProductName = "Firma B Mouse",
                    Price = 750m
                });
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Sepetler");
            entity.HasKey(cart => cart.Id);

            entity.Property(cart => cart.CompanyId)
                .HasColumnName("FirmaId");

            entity.Property(cart => cart.UserId)
                .HasColumnName("KullaniciId");

            entity.HasIndex(cart => new { cart.CompanyId, cart.UserId })
                .HasDatabaseName("IX_Sepetler_FirmaId_KullaniciId")
                .IsUnique();

            entity.HasIndex(cart => cart.UserId)
                .HasDatabaseName("IX_Sepetler_KullaniciId");

            entity.HasOne(cart => cart.Company)
                .WithMany(company => company.Carts)
                .HasForeignKey(cart => cart.CompanyId)
                .HasConstraintName("FK_Sepetler_Firmalar_FirmaId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cart => cart.User)
                .WithOne(user => user.Cart)
                .HasForeignKey<Cart>(cart => cart.UserId)
                .HasConstraintName("FK_Sepetler_Kullanicilar_KullaniciId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("SepetKalemleri");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.CartId)
                .HasColumnName("SepetId");

            entity.Property(item => item.ProductId)
                .HasColumnName("UrunId");

            entity.Property(item => item.Quantity)
                .HasColumnName("Miktar")
                .IsRequired();

            entity.HasIndex(item => new { item.CartId, item.ProductId })
                .HasDatabaseName("IX_SepetKalemleri_SepetId_UrunId")
                .IsUnique();

            entity.HasIndex(item => item.ProductId)
                .HasDatabaseName("IX_SepetKalemleri_UrunId");

            entity.HasOne(item => item.Cart)
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .HasConstraintName("FK_SepetKalemleri_Sepetler_SepetId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany(product => product.CartItems)
                .HasForeignKey(item => item.ProductId)
                .HasConstraintName("FK_SepetKalemleri_Urunler_UrunId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Siparisler");
            entity.HasKey(order => order.Id);

            entity.Property(order => order.CompanyId)
                .HasColumnName("FirmaId");

            entity.Property(order => order.UserId)
                .HasColumnName("KullaniciId");

            entity.Property(order => order.CreatedAt)
                .HasColumnName("OlusturmaTarihi");

            entity.Property(order => order.TotalAmount)
                .HasColumnName("ToplamTutar")
                .HasPrecision(18, 2);

            entity.HasIndex(order => new { order.CompanyId, order.UserId, order.CreatedAt })
                .HasDatabaseName("IX_Siparisler_FirmaId_KullaniciId_OlusturmaTarihi");

            entity.HasIndex(order => order.UserId)
                .HasDatabaseName("IX_Siparisler_KullaniciId");

            entity.HasOne(order => order.Company)
                .WithMany(company => company.Orders)
                .HasForeignKey(order => order.CompanyId)
                .HasConstraintName("FK_Siparisler_Firmalar_FirmaId")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .HasConstraintName("FK_Siparisler_Kullanicilar_KullaniciId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("SiparisKalemleri");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.OrderId)
                .HasColumnName("SiparisId");

            entity.Property(item => item.ProductId)
                .HasColumnName("UrunId");

            entity.Property(item => item.ProductCode)
                .HasColumnName("UrunKodu")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(item => item.ProductName)
                .HasColumnName("UrunAdi")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(item => item.UnitPrice)
                .HasColumnName("BirimFiyat")
                .HasPrecision(18, 2);

            entity.Property(item => item.LineTotal)
                .HasColumnName("KalemToplami")
                .HasPrecision(18, 2);

            entity.Property(item => item.Quantity)
                .HasColumnName("Miktar")
                .IsRequired();

            entity.HasIndex(item => item.OrderId)
                .HasDatabaseName("IX_SiparisKalemleri_SiparisId");

            entity.HasIndex(item => item.ProductId)
                .HasDatabaseName("IX_SiparisKalemleri_UrunId");

            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .HasConstraintName("FK_SiparisKalemleri_Siparisler_SiparisId")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany(product => product.OrderItems)
                .HasForeignKey(item => item.ProductId)
                .HasConstraintName("FK_SiparisKalemleri_Urunler_UrunId")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

