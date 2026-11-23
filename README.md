# Ders Notlarını Seslendiren Öğrenme Asistanı — Backend

C# / .NET 9 Web API, N-Tier (katmanlı) mimari, PostgreSQL 16, Redis, SignalR.

## Katmanlar

| Proje | Sorumluluk |
|---|---|
| `SesliAsistan.Entities` | Varlık (entity) sınıfları ve enum'lar (19 tablo) — hiçbir katmana bağımlı değil |
| `SesliAsistan.DataAccess` | EF Core `AppDbContext`, Fluent API konfigürasyonları, repository'ler, Unit of Work |
| `SesliAsistan.Business` | İş kuralları, DTO'lar, doğrulama (FluentValidation), JWT/BCrypt, metin çıkarma (PdfPig, OpenXML), NLP (TextRank, KeyTermExtractor), TTS ve GenAI |
| `SesliAsistan.API` | Controller'lar (51+ uç nokta), SignalR hub (`/hubs/notifications`), kimlik doğrulama, RBAC, Rate Limiting, Swagger |
| `SesliAsistan.Tests` | xUnit birim testleri (114 senaryo) |

Bağımlılık yönü: `API → Business → DataAccess → Entities`

## Çalıştırma

```bash
docker compose up -d                       # PostgreSQL + Redis
dotnet tool install --global dotnet-ef     # bir kez
dotnet ef database update -p src/SesliAsistan.DataAccess -s src/SesliAsistan.API
dotnet run --project src/SesliAsistan.API  # Swagger: https://localhost:7180/swagger
dotnet test                                # 114 birim testi çalıştırır
```

Redis kullanmak için `appsettings.json` → `ConnectionStrings:Redis` alanına `localhost:6379` yazın;
boş bırakılırsa bellek içi önbellek kullanılır.

Üretim ortamında `Jwt:Secret` değeri **asla** dosyaya yazılmamalı; ortam değişkeni
(`Jwt__Secret`) veya `dotnet user-secrets` ile verilmelidir.

## Hazır Uç Noktalar (v1 - 51+ Endpoint)

### 1. Kimlik Doğrulama ve Profil (FG-1)
- `POST /api/v1/auth/register` — Yeni kullanıcı kaydı
- `POST /api/v1/auth/login` — Giriş (JWT + refresh token rotasyonu)
- `POST /api/v1/auth/refresh` — Token yenileme
- `POST /api/v1/auth/logout` — Çıkış (token iptali)
- `POST /api/v1/auth/forgot-password` — Şifre sıfırlama kodu gönderme
- `POST /api/v1/auth/reset-password` — Kod ile şifre sıfırlama
- `GET/PUT/DELETE /api/v1/users/me` — Profil görüntüle / güncelle / hesabı sil (KVKK)
- `PUT /api/v1/users/me/password` — Şifre değiştir (oturumları kapat)

### 2. Belge Yükleme ve Konu Ağacı (FG-2, FG-5)
- `POST /api/v1/documents` — PDF/.docx yükleme (≤ 20 MB, 202 Accepted)
- `GET /api/v1/documents` — Sayfalı listeleme ve başlığa göre arama
- `GET /api/v1/documents/{id}` — Belge ayrıntısı, işleme durumu ve konu ağacı
- `GET /api/v1/documents/{id}/sections/{sid}` — Bölüm metni
- `POST /api/v1/documents/{id}/reprocess` — Belgeyi yeniden işleme
- `DELETE /api/v1/documents/{id}` — Belge ve dosyasını silme

### 3. Seslendirme (TTS) ve Dinleme Konumu (FG-3)
- `GET /api/v1/documents/{id}/sections/{sid}/audio` — Bölüm seslendirmesi (önbellek destekli)
- `GET /api/v1/audio/{audioId}` — Ses dosyasını akış (stream) olarak çalma (Range header)
- `GET /api/v1/tts/voices` — Kullanılabilir Türkçe sesler
- `PUT /api/v1/documents/{id}/progress` — Dinleme konumunu kaydetme
- `GET /api/v1/documents/{id}/progress` — Kaldığı yerden devam bilgisi

### 4. Sesli Özet (FG-4)
- `POST /api/v1/documents/{id}/sections/{sid}/summary` — TextRank tabanlı özet üretme (kısa/orta/detaylı)
- `GET /api/v1/summaries/{id}` — Özet metni
- `GET /api/v1/summaries/{id}/audio` — Özet seslendirmesi

### 5. Bilgi Kartları (FG-6)
- `POST /api/v1/documents/{id}/sections/{sid}/flashcards` — Bölümden kilit kavram çıkarımı (TF-IDF + F5) ve bilgi kartı destesi üretme
- `GET /api/v1/flashcard-decks` — Desteleri listeleme
- `GET /api/v1/flashcard-decks/{deckId}` — Deste ve kartlarını görüntüleme
- `PUT /api/v1/flashcards/{cardId}` — Kartı düzenleme
- `DELETE /api/v1/flashcard-decks/{deckId}` — Desteyi silme
- `GET /api/v1/flashcards/{cardId}/audio` — Kartın sesli hâli (ön / arka)

### 6. GenAI ile Çoktan Seçmeli Testler (FG-6)
- `POST /api/v1/documents/{id}/quizzes` — Groq API ile test üretme isteği (202 Accepted)
- `GET /api/v1/quizzes/{quizId}` — Test durumu ve soruları (doğru cevap gizli)
- `POST /api/v1/quizzes/{quizId}/attempts` — Cevapları gönderme ve sunucu puanlaması
- `GET /api/v1/quizzes/{quizId}/attempts` — Başarı geçmişi

### 7. İşbirlikçi Ağ ve Sosyal Paylaşım (FG-7)
- `GET /api/v1/users/search?q=` — Kullanıcı arama (e-posta gizli)
- `POST /api/v1/friends/requests` — Arkadaşlık isteği gönderme
- `PUT /api/v1/friends/requests/{id}` — İsteği kabul etme / reddetme
- `GET /api/v1/friends` — Arkadaş listesi ve bekleyen istekler
- `DELETE /api/v1/friends/{userId}` — Arkadaşlıktan çıkarma / engelleme
- `POST /api/v1/shares` — İçeriği gizlilik düzeyiyle (Private/Friends/Group/Public) paylaşma
- `PUT /api/v1/shares/{id}` — Gizlilik düzeyini güncelleme
- `GET /api/v1/feed?cursor=` — İmleç tabanlı sosyal akış
- `POST /api/v1/shares/{id}/copy` — "Kütüphaneme ekle" ile bağımsız kopyalama
- `POST /api/v1/groups` — Çalışma grubu oluşturma (8 karakterlik davet kodu)
- `POST /api/v1/groups/join` — Davet koduyla gruba katılma (maks 30 üye)
- `GET /api/v1/groups/{id}` — Grup bilgisi ve üyeleri
- `GET /api/v1/notifications` — Bildirimleri listeleme
- `PUT /api/v1/notifications/read` — Bildirimleri okundu işaretleme
- `WS /hubs/notifications` — SignalR anlık bildirim iletimi

### 8. Yönetim Paneli ve Moderasyon (FG-8)
- `POST /api/v1/reports` — İçerik şikâyet etme (günlük 5 sınır; 3 şikâyette otomatik UnderReview)
- `GET /api/v1/admin/reports` — Şikâyet kuyruğu (yalnızca Admin)
- `PUT /api/v1/admin/reports/{id}` — Moderasyon kararı (Kaldır / Reddet)
- `PUT /api/v1/admin/users/{id}/status` — Kullanıcıyı askıya alma / açma (oturumlar iptal)
- `GET /api/v1/admin/stats` — Genel sistem ve yapay zeka kullanım istatistikleri
