# B4B SaaS Example


## Teknoloji Sürümleri

- .NET: `net10.0`
- EF Core SQL Server: `10.0.12`
- EF Core Design: `10.0.12`
- dotnet-ef: `10.0.12`

## Projeler

- `B4B.Api`: Ortak ASP.NET Core Web API
- `B4B.Web`: Ortak ASP.NET Core MVC uygulaması

Her firma için ayrı MVC projesi yoktur. Aynı `B4B.Web` projesi iki ayrı portta çalıştırılır.

Kod tarafındaki teknik sınıf ve dosya adları İngilizcedir (`Company`, `User`, `Product`, `Cart`, `Order` gibi). Veritabanı tablo/kolon adları, mevcut migration sözleşmesi bozulmasın diye Türkçe olarak korunur.

## Portlar

- API: `https://localhost:7001`
- Firma A MVC: `https://localhost:7101`
- Firma B MVC: `https://localhost:7201`
- Ortak yönetim paneli: `https://localhost:7301`

## Veritabanı

Varsayilan connection string:

```json
"DefaultConnection": "Server=(localdb)\\B4BLocalDb;Database=B4BSaaSExampleDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Bu çalışma alanında `B4BLocalDb` adlı LocalDB instance kullanılır. SQL Server LocalDB kullanmıyorsanız `B4B.Api/appsettings.json` içindeki connection string'i kendi MSSQL sunucunuza göre değiştirin.

## Migration Uygulama

```powershell
dotnet ef database update --project B4B.Api\B4B.Api.csproj --startup-project B4B.Api\B4B.Api.csproj
```

Not: `20261006084009_ConvertIdsToGuid` migration'ının `Down` işlemi desteklenmez. Eski int ID değerleri GUID değerlere taşındığı için bu dönüşüm güvenli biçimde geri alınamaz.

Bu komut `Firmalar` tablosunu oluşturur ve şu kayıtları ekler:

- `Firma A`, `localhost:7101`
- `Firma B`, `localhost:7201`

API ilk çalıştığında iki demo kullanıcıyı eksikse ekler. Şifreler veritabanında hash olarak saklanır.

- Firma A: `demo` / `FirmaA123!`
- Firma B: `demo` / `FirmaB123!`

API ilk çalıştığında her firma için bir admin kullanıcıyı da eksikse ekler. Admin girişi firma domain'iyle değil firma koduyla yapılır.

- Firma A admin: `FIRMAA` / `admin` / `AdminA123!`
- Firma B admin: `FIRMAB` / `admin` / `AdminB123!`

Admin paneli için eklenen migration:

- `20261006095339_AddAdminPanel`

JWT imzalama anahtarı kaynak kodda tutulmaz. Geliştirme için User Secrets kullanılır:

```powershell
dotnet user-secrets set "Jwt:Key" "gelistirme-icin-uzun-bir-secret" --project B4B.Api\B4B.Api.csproj
```

`Jwt:Key` en az 32 UTF-8 byte uzunluğunda olmalıdır. Uzunluk kontrolü tek başına rastgelelik sağlamaz; geliştirme ve üretim ortamlarında güçlü, rastgele ve tahmin edilemeyen bir anahtar kullanılmalıdır.

Normal giriş ve admin panel girişi için basit IP bazlı rate limit uygulanır: aynı IP adresinden 1 dakikada toplam 10 giriş isteği kabul edilir. MVC giriş istekleri sunucu üzerinden API'ye gittiği için bu sınır aynı MVC instance'ındaki kullanıcılar arasında paylaşılır. Bu ödev kapsamında basit sınırlandırma yeterli görülmüştür.

## Çalıştırma

Dört ayrı terminal açın.

API:

```powershell
dotnet run --project B4B.Api\B4B.Api.csproj --launch-profile https
```

Firma A MVC:

```powershell
dotnet run --project B4B.Web\B4B.Web.csproj --launch-profile FirmaA
```

Firma B MVC:

```powershell
dotnet run --project B4B.Web\B4B.Web.csproj --launch-profile FirmaB
```

Ortak yönetim paneli:

```powershell
dotnet run --project B4B.Web\B4B.Web.csproj --launch-profile Admin
```

Panel adresi:

```text
https://localhost:7301/Admin/Login
```

## Akış

MVC kendi çalıştığı adresi `HttpContext.Request.Host.Value` ile alır.

Örnek:

```text
localhost:7101
```

Sonra API'ye sunucu tarafında `HttpClient` ile istek atarken bu bilgiyi header olarak ekler:

```http
X-Client-Host: localhost:7101
```

API kendi adresi olan `localhost:7001` ile firma bulmaya çalışmaz. Gelen MVC adresini `Firmalar.Domain` alanında arar ve yalnızca `Id`, `FirmaAdi`, `ConfigDegeri` bilgilerini döner.

Config yanıtına şifre, connection string veya gizli bilgi eklenmez.

## Güvenlik Notu

Bu ilk aşamada domain/header bilgisi sadece firma seçimi içindir. Tek başına kimlik doğrulama sağlamaz. MVC tarafında izin verilen adresler `AllowedClientHosts` ile sınırlandırılmıştır, API tarafında da gelen adres veritabanındaki kayıtlı domain ile eşleştirilir.

Login sonrası API JWT üretir. MVC JWT'yi tarayıcıya yazmaz; sunucu tarafında session içinde saklar. Tarayıcıya HttpOnly ve Secure cookie verilir. Korunan API uç noktaları firma bilgisini istemciden gelen domain bilgisinden değil, doğrulanmış JWT içindeki `firma_id` claim'inden alır.

## Kapsam Notu

`FirmaId + KullaniciAdi` alanları veritabanında benzersizdir. Aynı firmada aynı kullanıcı adı tekrar eklenirse veritabanı unique kuralı bunu engeller. Bu ödev kapsamında kullanıcı oluşturma ekranı veya kullanıcı oluşturma endpoint'i yoktur; bu yüzden "Bu firmada bu kullanıcı adı zaten kullanılıyor." mesajını kullanıcıya gösteren bir akış uygulanmamıştır.
