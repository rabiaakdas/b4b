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

## Elasticsearch ve Redis Yerel Altyapı

Bu bölüm yalnızca yerel geliştirme içindir. Elasticsearch ve Redis portları sadece `127.0.0.1` üzerinden açılır. Elasticsearch güvenliği ve Redis şifresi bu yerel Docker Compose dosyasında kapalıdır; üretim ortamında TLS, kimlik doğrulama ve secret yönetimi kullanılmalıdır.

Servisleri başlatma:

```powershell
docker compose -f docker-compose.infrastructure.yml up -d
```

Servis durumunu kontrol etme:

```powershell
docker compose -f docker-compose.infrastructure.yml ps
curl.exe http://localhost:9200
curl.exe http://localhost:9200/_cluster/health
docker exec b4b-redis redis-cli ping
```

Yerel bağlantı adresleri:

- Elasticsearch: `http://localhost:9200`
- Redis: `localhost:6379`

API ürün araması Elasticsearch üzerinden çalışır. Redis aynı firma, arama metni, sayfa ve sayfa boyutu için ürün arama sonucunu kısa süreli önbelleğe alır. SQL Server ürün kayıtları asıl veri kaynağı olarak kalır; sepet ve sipariş işlemleri SQL'deki ürünleri kullanmaya devam eder.

API ayarları:

```json
"Elasticsearch": {
  "Url": "http://localhost:9200",
  "ProductIndex": "b4b-products",
  "ProductSearchSize": 100
},
"Redis": {
  "Configuration": "127.0.0.1:6379",
  "ProductSearchCacheSeconds": 900
}
```

Mevcut SQL ürünlerini Elasticsearch'e aktarma:

```powershell
dotnet run --project B4B.Api\B4B.Api.csproj --launch-profile https -- --index-products
```

Aktarım tekrar çalıştırıldığında aynı ürünler çoğalmaz; Elasticsearch belge ID'si ürün GUID değeridir. Başarılı aktarım sonunda Redis arama cache sürümü artırılır, böylece eski ürün arama cache kayıtları kullanılmaz. `KEYS` veya `FLUSHALL` kullanılmaz.

Ürün arama endpoint'i mevcut adresini ve query parametresini korur:

```text
GET https://localhost:7001/api/urunler?arama=...
```

Boş aramada sadece JWT içindeki firma bilgisine ait ürünler döner. Sonuçlar tek istekte en fazla 100 ürün olacak şekilde sınırlandırılır; `sayfa` ve `sayfaBoyutu` query parametreleri MVC ürün ekranında kullanılır.

Derin sayfalama bu örnekte Elasticsearch `from + size` yaklaşımıyla sınırlıdır. `sayfa` ve `sayfaBoyutu` hesabı 10.000 sonucunu aşarsa API kontrollü `400` döndürür. Üretim ortamında çok derin sayfalama için `search_after` gibi ayrı bir yaklaşım tercih edilmelidir.

Arama yanıtı header'ları:

- `X-Total-Count`: Toplam eşleşme sayısı.
- `X-Search-Took-Ms`: Yalnızca Elasticsearch çağrısının uygulama tarafında ölçülen süresi. Cache HIT durumunda Elasticsearch çağrısı yapılmadığı için `0` döner.
- `X-Search-Duration-Ms`: Redis cache kontrolü dahil güncel API arama işleminin toplam süresi.
- `X-Page`: Dönen sayfa.
- `X-Page-Size`: Dönen sayfa boyutu.
- `X-Cache`: `HIT` veya `MISS`.

Redis erişilemezse ürün araması Elasticsearch üzerinden devam eder ve hata loglanır. Elasticsearch de erişilemezse API kontrollü biçimde `503` döner.

### Büyük Deneme Ürünü Üretme

Deneme ürünleri gerçek SQL Server ürün kayıtları olarak oluşturulur ve ardından Elasticsearch'e bulk aktarılır. Uygulama başlangıcında otomatik çalışmaz; açıkça komut verilmelidir. Ürün kodları firma içinde benzersizdir ve `B4BTEST` önekiyle ayırt edilir.

Önce küçük doğrulama için 1.000 kayıt:

```powershell
dotnet run --project B4B.Api\B4B.Api.csproj --launch-profile https -- --seed-test-products --count 1000 --batch-size 500
```

Toplam hedefi 1 milyon deneme ürününe çıkarmak için:

```powershell
dotnet run --project B4B.Api\B4B.Api.csproj --launch-profile https -- --seed-test-products --count 1000000 --batch-size 5000
```

Komut tekrar çalıştırıldığında aynı deneme ürünleri çoğaltılmaz. SQL'de eksik kayıtlar küçük partilerle eklenir, Elasticsearch belge ID'si ürün GUID değeri olduğu için aynı ürün tekrar indekslenirse güncellenir. Aktarım sonunda Redis ürün arama cache sürümü artırılır.

Örnek ölçüm sorguları:

```text
GET /api/urunler?arama=Deneme%20Profesyonel&sayfa=1&sayfaBoyutu=20
GET /api/urunler?arama=B4BTEST-A-0004321&sayfa=1&sayfaBoyutu=20
GET /api/urunler?arama=bulunamayacak-urun-xyz-2&sayfa=1&sayfaBoyutu=20
```

Servisleri durdurma:

```powershell
docker compose -f docker-compose.infrastructure.yml down
```

Kalıcı Elasticsearch ve Redis verilerini de silmek gerekirse:

```powershell
docker compose -f docker-compose.infrastructure.yml down -v
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
