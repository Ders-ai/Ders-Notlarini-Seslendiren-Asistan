/**
 * API ayarları. Değerler .env dosyasından gelir (bkz. .env.example).
 * Backend hazır olana kadar EXPO_PUBLIC_USE_MOCK=true ile örnek verilerle çalışılır.
 */
export const apiConfig = {
  baseUrl: process.env.EXPO_PUBLIC_API_URL ?? 'http://10.0.2.2:5180',
  useMock: (process.env.EXPO_PUBLIC_USE_MOCK ?? 'true') === 'true',
  timeoutMs: 15_000,
} as const;
