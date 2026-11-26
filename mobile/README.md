# DersAI — Mobil Uygulama

React Native (Expo SDK 58) + TypeScript. Android öncelikli; hedef Google Play.

Tasarım: Figma › "02 · Final Tasarım (Nar çiçeği)" — ekran numaraları (2.1, 3.3 …) kodda yorum olarak geçer.

## Kurulum

```bash
cd mobile
npm install
cp .env.example .env      # API adresi ve örnek veri ayarı
npx expo run:android      # emülatörde veya bağlı telefonda geliştirme sürümü
```

Gerekenler: Node 22+, JDK 17 (Zulu), Android Studio (SDK + emülatör), `JAVA_HOME` ve `ANDROID_HOME` tanımlı.

## Klasör yapısı

| Klasör | İçerik |
|---|---|
| `app/` | Ekranlar (Expo Router, dosya adı = yol) |
| `app/(tabs)/` | Alt menüdeki 5 sekme: Bugün, Kitaplık, Akış, Arkadaşlar, Profil |
| `src/theme/` | Renkler (6 renk + beyaz), yazı tipleri, ölçüler, açık/koyu tema |
| `src/components/ui/` | Ortak bileşenler: Text, Button, Card, Input, Chip, Toggle, ProgressBar … |
| `src/components/navigation/` | Ada şeklindeki alt menü |
| `src/api/` | Axios istemcisi, JWT saklama (SecureStore), API ayarları |
| `src/types/` | Backend DTO'larının TypeScript karşılıkları |
| `src/store/` | Uygulama durumu (Zustand) |

## Kurallar

- Renk yalnızca `src/theme` üzerinden kullanılır; ekranlarda sabit renk kodu yazılmaz.
- API adresi ve anahtarlar koda yazılmaz, `.env` içinde durur (`.env` depoya gönderilmez).
- Backend'de bir DTO değişirse `src/types/api.ts` güncellenir.

## Komutlar

| Komut | Ne yapar |
|---|---|
| `npm start` | Metro geliştirme sunucusu |
| `npm run android` | Android'de aç |
| `npm run typecheck` | TypeScript kontrolü |
