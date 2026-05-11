# OutboxPatternPOC

## Role

Bu proje kapsamında sistem şu bakış açısıyla tasarlanmıştır:

- Kıdemli Software Architect
- Distributed Systems ve Event-Driven Architecture uzmanı
- .NET (C#) backend geliştiricisi
- Redis ve messaging sistemlerinde deneyimli engineer

 

## Business Requirements

Sistemin temel iş ihtiyaçları:

- Kullanıcı bir Order oluşturabilmeli
- Order oluşturulduğunda sistem veri kaybı olmadan event üretmeli
- Event’ler güvenilir şekilde downstream sistemlere iletilmeli
- Mikroservis mimarisinde event consistency garanti edilmeli
- Order oluşturma ve event üretimi atomik (tek işlem gibi) davranmalı

İş hedefleri:
- Order kaybolmamalı
- Event kaybolmamalı
- Duplicate event riskine karşı dayanıklı olmalı
- Asenkron event processing desteklenmeli

 

## Technical Details

### Teknoloji Yığını
- .NET 10 (Minimal API)
- C#
- Entity Framework Core (SQL Server)
- Redis (Stream-based messaging)
- Docker + Docker Compose

 

### Ana Bileşenler

#### Order Service
- Order oluşturur
- Aynı transaction içinde Outbox tablosuna event yazar

#### Outbox Table
- Event’leri geçici olarak saklar
- Güvenilir delivery mekanizmasının temelidir

#### Background Worker
- Outbox kayıtlarını periyodik olarak okur
- Redis Stream’e publish eder

#### Redis Stream
- Message broker simülasyonu yapar
- Event tüketimi için kullanılabilir

 

### Event Flow

1. Client Order oluşturur
2. Order DB’ye yazılır
3. Outbox tablosuna event eklenir
4. Worker Outbox’u okur
5. Redis Stream’e event publish edilir
6. Consumer event’i alır

 

## Strategy

- Her faz için başarı kriterleri tanımlanmış bir plan yaz. Proje iskeletini (.gitignore dahil) ve kapsamlı unit testleri içermeli
- Planı uygulayarak tüm kriterlerin karşılandığını doğrula
- Kapsamlı entegrasyon testleri yap, hataları düzelt
- MVP tamamlanıp test edilmiş, server çalışır ve kullanıma hazır olana kadar bitirme

### Reliability First
- Event publishing DB transaction’a bağlanır
- At least once delivery garanti edilir

### Decoupling
- Order creation ve event publishing ayrılır
- Worker ile async processing sağlanır

### Simplicity
- Kafka/RabbitMQ yerine Redis Stream kullanılır
- Production benzeri ama hafif yapı kurulur

### Observability Ready
- Event flow loglanabilir
- Redis stream üzerinden debug yapılabilir

 

## Coding Standards

### Genel Kurallar
- Clean Code prensipleri uygulanır
- SOLID prensiplerine uyulur
- Dependency Injection kullanılır
- Katmanlar net ayrılır
- Basit tut - ASLA aşırı mühendislik yapma, HER ZAMAN sadeleştir, gereksiz defensive programming yapma. Ekstra özellik ekleme
- Kısa ve öz ol. README minimal olmalı. EMOJİ ASLA KULLANMA
 

### Naming Conventions
- PascalCase: Class ve method isimleri
- camelCase: local variables
- Interface isimleri I prefix ile başlar

 

### Architecture Rules
- Endpoint/controller sadece request alır
- Business logic service katmanında bulunur
- Data access DbContext üzerinden yapılır
- Background işlemler Worker içinde çalışır

 

### Outbox Pattern Kuralları
- Event DB transaction içinde yazılmalıdır
- Worker polling mekanizması kullanır
- Processed flag ile idempotency sağlanır

 

### Redis Usage
- Redis Stream kullanılmalıdır
- Event payload JSON formatında olmalıdır
- Stream key: outbox-stream

 