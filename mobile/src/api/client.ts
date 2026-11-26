import axios, { AxiosError } from 'axios';

import { tokenStorage } from './tokenStorage';
import { apiConfig } from './config';

/** Tüm istekler /api/v1 altına gider (backend README). */
export const api = axios.create({
  baseURL: `${apiConfig.baseUrl}/api/v1`,
  timeout: apiConfig.timeoutMs,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(async (config) => {
  const token = await tokenStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

/** Backend'in hata gövdesinden kullanıcıya gösterilecek Türkçe mesajı çıkarır. */
export function errorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    if (!error.response) return 'Sunucuya ulaşılamadı. İnternet bağlantını kontrol et.';
    const data = error.response.data as { message?: string; title?: string } | undefined;
    return data?.message ?? data?.title ?? 'Beklenmeyen bir hata oluştu.';
  }
  return 'Beklenmeyen bir hata oluştu.';
}
