import type { TextStyle } from 'react-native';

/** Yüklenen yazı tipi adları (app/_layout.tsx içinde useFonts ile yüklenir). */
export const fonts = {
  serif: 'Fraunces_600SemiBold',
  regular: 'Manrope_400Regular',
  medium: 'Manrope_500Medium',
  semibold: 'Manrope_600SemiBold',
  bold: 'Manrope_700Bold',
} as const;

/** Figma'daki 270 px çizimler 390 px ekrana göre ölçeklenmiş değerlerdir. */
export const typography = {
  h1: { fontFamily: fonts.serif, fontSize: 32, lineHeight: 38 },
  h2: { fontFamily: fonts.serif, fontSize: 24, lineHeight: 30 },
  h3: { fontFamily: fonts.serif, fontSize: 20, lineHeight: 26 },
  body: { fontFamily: fonts.regular, fontSize: 16, lineHeight: 24 },
  bodyStrong: { fontFamily: fonts.semibold, fontSize: 16, lineHeight: 24 },
  label: { fontFamily: fonts.semibold, fontSize: 15, lineHeight: 20 },
  caption: { fontFamily: fonts.regular, fontSize: 13, lineHeight: 18 },
  overline: { fontFamily: fonts.bold, fontSize: 12, lineHeight: 16, letterSpacing: 1.2 },
} satisfies Record<string, TextStyle>;

export type TypographyVariant = keyof typeof typography;
