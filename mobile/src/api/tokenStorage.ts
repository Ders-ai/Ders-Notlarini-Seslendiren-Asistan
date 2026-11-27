import * as SecureStore from 'expo-secure-store';

const ACCESS = 'dersai.accessToken';
const REFRESH = 'dersai.refreshToken';

/** JWT ve yenileme anahtarı cihazın güvenli deposunda (Android Keystore) tutulur. */
export const tokenStorage = {
  getAccessToken: () => SecureStore.getItemAsync(ACCESS),
  getRefreshToken: () => SecureStore.getItemAsync(REFRESH),
  async save(accessToken: string, refreshToken: string) {
    await SecureStore.setItemAsync(ACCESS, accessToken);
    await SecureStore.setItemAsync(REFRESH, refreshToken);
  },
  async clear() {
    await SecureStore.deleteItemAsync(ACCESS);
    await SecureStore.deleteItemAsync(REFRESH);
  },
};
