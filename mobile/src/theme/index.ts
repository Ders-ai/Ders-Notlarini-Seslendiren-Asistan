import { useColorScheme } from 'react-native';

import { useSettingsStore } from '@/store/settings';
import { darkColors, lightColors, type ThemeColors } from './colors';

export { palette, lightColors, darkColors } from './colors';
export type { ThemeColors } from './colors';
export { fonts, typography } from './typography';
export type { TypographyVariant } from './typography';

/** Ölçüler — Figma "Ölçüler" kartı. */
export const spacing = { xs: 4, sm: 8, md: 12, lg: 16, xl: 22, xxl: 32 } as const;
export const radius = { chip: 99, input: 16, button: 20, card: 22, sheet: 36, screen: 48 } as const;
/** Dokunma alanı en az 44 pt olmalı. */
export const minTouch = 44;

export type Theme = { dark: boolean; colors: ThemeColors };

/** Ayarlardaki tema seçimine (Sistem / Açık / Koyu) göre aktif renkleri döndürür. */
export function useTheme(): Theme {
  const system = useColorScheme();
  const mode = useSettingsStore((s) => s.themeMode);
  const dark = mode === 'system' ? system === 'dark' : mode === 'dark';
  return { dark, colors: dark ? darkColors : lightColors };
}
