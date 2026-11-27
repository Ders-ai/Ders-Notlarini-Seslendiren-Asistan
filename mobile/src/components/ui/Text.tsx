import { Text as RNText, type TextProps as RNTextProps } from 'react-native';

import { typography, useTheme, type TypographyVariant } from '@/theme';

export type TextProps = RNTextProps & {
  variant?: TypographyVariant;
  tone?: 'default' | 'secondary' | 'accent' | 'inverse';
  align?: 'left' | 'center' | 'right';
};

export function Text({ variant = 'body', tone = 'default', align, style, ...rest }: TextProps) {
  const { colors } = useTheme();
  const color =
    tone === 'secondary' ? colors.textSecondary : tone === 'accent' ? colors.accent : tone === 'inverse' ? colors.onPrimary : colors.text;
  return <RNText {...rest} style={[typography[variant], { color, textAlign: align }, style]} />;
}
