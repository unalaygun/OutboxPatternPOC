# Outbox Pattern POC

Transactional Outbox Pattern uygulamasini gosteren, .NET 10 Minimal API tabanli bir Proof of Concept projesidir. Sistem, siparis olusturma ve ilgili event'in guvenilir bir sekilde mesaj kuyruguna (Redis Stream) iletilmesini garanti eder.

## Mimari Yaklasim

Mikroservis mimarilerinde veritabani guncellemesi ile event yayinlama isleminin atomik olmasi kritik bir ihtiyactir. Bu projede Transactional Outbox Pattern kullanilarak su akis saglanmistir:

1. **Atomic Write**: `Order` verisi ve `OutboxMessage` verisi ayni veritabani transaction'i icinde SQL Server'a yazilir.
2. **Polling**: Bir background worker (`OutboxWorker`) veritabanindaki islenmemis mesajlari belirli araliklarla kontrol eder.
3. **Publishing**: Mesajlar Redis Stream'e iletilir ve basarili gonderim sonrasinda veritabaninda islendi olarak isaretlenir.

## Teknoloji Yigini

- .NET 10 (Minimal API)
- Entity Framework Core (SQL Server)
- Redis Streams (Messaging)
- Docker & Docker Compose
- xUnit & Moq & FluentAssertions (Testing)

## Proje Yapisi

- `src/OutboxPatternPOC.Api`: Ana API projesi, domain modelleri, servisler ve background worker.
- `tests/OutboxPatternPOC.Tests`: Unit ve integration testleri.
- `docker-compose.yml`: SQL Server ve Redis altyapisi.

## Kurulum ve Calistirma

### Altyapinin Baslatilmasi

Projenin ana dizininde su komutu calistirarak SQL Server ve Redis container'larini ayaga kaldirin:

```bash
docker-compose up -d
```

### Uygulamanin Calistirilmasi

```bash
dotnet run --project src/OutboxPatternPOC.Api/OutboxPatternPOC.Api.csproj
```

### Testlerin Kosulmasi

Proje %80+ test coverage hedefiyle unit ve integration testlerini icerir:

```bash
dotnet test
```

## Kullanim Ornegi

### Siparis Olusturma

```bash
curl -X POST http://localhost:5000/api/orders \
     -H "Content-Type: application/json" \
     -d '{
       "customerName": "John Doe",
       "totalAmount": 950.00
     }'
```

### Redis Stream Kontrolu

Background worker mesajlari Redis'e ilettikten sonra, iletilen event'leri su komutla gorebilirsiniz:

```bash
docker exec -it outbox-redis redis-cli XRANGE outbox-stream - +
```

## Güvenilirlik Garantileri

- **At-least-once Delivery**: Mesajlar Redis'e basariyla yazilmadan veritabaninda islendi olarak isaretlenmez.
- **Data Integrity**: SQL Server transaction yonetimi sayesinde siparis kaydi ile outbox kaydi asla birbirinden ayri dusmez.
- **Resilience**: Redis baglantisi kopsa dahi worker mesajlari veritabaninda tutmaya devam eder ve baglanti geldiginde kaldigi yerden devam eder.
