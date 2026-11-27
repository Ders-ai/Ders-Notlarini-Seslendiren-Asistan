import type { ReactNode } from 'react';
import { View } from 'react-native';

import { useTheme } from '@/theme';

/** Listelerdeki kare ikon kutusu (Figma: Kart zemin, 14 köşe). */
export function IconBox({ children, size = 46, tone = 'muted' }: { children: ReactNode; size?: number; tone?: 'muted' | 'accent' }) {
  const { colors } = useTheme();
  return (
    <View
      style={{
        width: size,
        height: size,
        borderRadius: size * 0.3,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: tone === 'accent' ? colors.border : colors.muted,
      }}
    >
      {children}
    </View>
  );
}
