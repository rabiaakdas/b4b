# B4B SaaS Example


## Teknoloji Surumleri

- .NET: `net10.0`
- EF Core SQL Server: `10.0.12`
- EF Core Design: `10.0.12`
- dotnet-ef: `10.0.12`

## Projeler

- `B4B.Api`: Ortak ASP.NET Core Web API
- `B4B.Web`: Ortak ASP.NET Core MVC uygulamasi

Her firma için ayrı MVC projesi yoktur. Aynı `B4B.Web` projesi iki ayrı portta çalıştırılır.

## Portlar

- API: `https://localhost:7001`
- Firma A MVC: `https://localhost:7101`
- Firma B MVC: `https://localhost:7201`

## Veritabani

Varsayilan connection string:

```json
"DefaultConnection": "Server=(localdb)\\B4BLocalDb;Database=B4BSaaSExampleDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Bu çalışma alanında `B4BLocalDb` adlı LocalDB instance kullanılır. SQL Server LocalDB kullanmıyorsanız `B4B.Api/appsettings.json` içindeki connection string'i kendi MSSQL sunucunuza göre değiştirin.

## Migration Uygulama

```powershell
dotnet ef database update --project B4B.Api\B4B.Api.csproj --startup-project B4B.Api\B4B.Api.csproj
```

Bu komut `Firmalar` tablosunu olusturur ve su kayitlari ekler:

- `Firma A`, `localhost:7101`
- `Firma B`, `localhost:7201`

API ilk çalıştığında iki demo kullanıcıyı eksikse ekler. Şifreler veritabanında hash olarak saklanır.

- Firma A: `demo` / `FirmaA123!`
- Firma B: `demo` / `FirmaB123!`

JWT imzalama anahtari kaynak kodda tutulmaz. Gelistirme icin User Secrets kullanilir:

```powershell
dotnet user-secrets set "Jwt:Key" "gelistirme-icin-uzun-bir-secret" --project B4B.Api\B4B.Api.csproj
```

## Calistirma

Üç ayrı terminal açın.

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

## Akis

MVC kendi çalıştığı adresi `HttpContext.Request.Host.Value` ile alır.

Ornek:

```text
localhost:7101
```

Sonra API'ye sunucu tarafinda `HttpClient` ile istek atarken bu bilgiyi header olarak ekler:

```http
X-Client-Host: localhost:7101
```

API kendi adresi olan `localhost:7001` ile firma bulmaya çalışmaz. Gelen MVC adresini `Firmalar.Domain` alanında arar ve yalnızca `Id`, `FirmaAdi`, `ConfigDegeri` bilgilerini döner.

Config yanıtına şifre, connection string veya gizli bilgi eklenmez.

## Guvenlik Notu

Bu ilk aşamada domain/header bilgisi sadece firma seçimi içindir. Tek başına kimlik doğrulama sağlamaz. MVC tarafında izin verilen adresler `AllowedClientHosts` ile sınırlandırılmıştır, API tarafında da gelen adres veritabanındaki kayıtlı domain ile eşleştirilir.

Login sonrası API JWT üretir. MVC JWT'yi tarayıcıya yazmaz; sunucu tarafında session içinde saklar. Tarayıcıya HttpOnly ve Secure cookie verilir. Korunan API uç noktaları firma bilgisini istemciden gelen domain bilgisinden değil, doğrulanmış JWT içindeki `firma_id` claim'inden alır.
