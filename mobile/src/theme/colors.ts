/**
 * DersAI renk paleti — Figma "Bölüm 0 · Tasarım sistemi".
 * Uygulamada bu 6 renk ve kart yüzeyleri için beyaz dışında renk kullanılmaz.
 */
export const palette = {
  zemin: '#F5F1EF',
  kart: '#EBE0DB',
  vurgu: '#E7D9D3',
  nar: '#C03D2B',
  ana: '#382B29',
  metin2: '#5E5552',
  beyaz: '#FFFFFF',
} as const;

export type ThemeColors = {
  /** Ekran arka planı */
  background: string;
  /** Kart ve alan yüzeyi */
  surface: string;
  /** İkon kutusu, seçim çubuğu, etiket */
  muted: string;
  /** Çizgi, boş ilerleme, seçili sekme zemini */
  border: string;
  /** Oynat, ilerleme, seçili ikon, hata, silme */
  accent: string;
  /** Başlık ve ana metin */
  text: string;
  /** İkincil metin, pasif ikon */
  textSecondary: string;
  /** Ana (birincil) buton zemini */
  primary: string;
  /** Ana buton üzerindeki metin */
  onPrimary: string;
  /** Alttan açılan pencerelerin arkasındaki karartma */
  overlay: string;
};

export const lightColors: ThemeColors = {
  background: palette.zemin,
  surface: palette.beyaz,
  muted: palette.kart,
  border: palette.vurgu,
  accent: palette.nar,
  text: palette.ana,
  textSecondary: palette.metin2,
  primary: palette.ana,
  onPrimary: palette.zemin,
  overlay: 'rgba(56,43,41,0.55)',
};

export const darkColors: ThemeColors = {
  background: palette.ana,
  surface: 'rgba(94,85,82,0.45)',
  muted: 'rgba(94,85,82,0.45)',
  border: palette.metin2,
  accent: palette.nar,
  text: palette.zemin,
  textSecondary: 'rgba(231,217,211,0.7)',
  primary: palette.zemin,
  onPrimary: palette.ana,
  overlay: 'rgba(0,0,0,0.55)',
};
