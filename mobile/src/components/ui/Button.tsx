import type { ReactNode } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View, type PressableProps } from 'react-native';

import { minTouch, radius, spacing, useTheme } from '@/theme';
import { Text } from './Text';

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost';

export type ButtonProps = Omit<PressableProps, 'children'> & {
  title: string;
  variant?: Variant;
  icon?: ReactNode;
  loading?: boolean;
};

export function Button({ title, variant = 'primary', icon, loading, disabled, style, ...rest }: ButtonProps) {
  const { colors } = useTheme();
  const isDisabled = disabled || loading;

  const bg = {
    primary: colors.primary,
    secondary: colors.surface,
    danger: colors.accent,
    ghost: 'transparent',
  }[variant];
  const fg = variant === 'primary' ? colors.onPrimary : variant === 'danger' ? '#FFFFFF' : colors.text;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: !!isDisabled, busy: !!loading }}
      disabled={isDisabled}
      style={(state) => [
        styles.base,
        { backgroundColor: isDisabled && variant === 'primary' ? colors.border : bg },
        variant === 'secondary' && { borderWidth: 1, borderColor: colors.border },
        state.pressed && { opacity: 0.85 },
        typeof style === 'function' ? style(state) : style,
      ]}
      {...rest}
    >
      {loading ? (
        <ActivityIndicator color={fg} />
      ) : (
        <View style={styles.row}>
          {icon}
          <Text variant="label" style={{ color: isDisabled && variant === 'primary' ? colors.textSecondary : fg }}>
            {title}
          </Text>
        </View>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    minHeight: Math.max(minTouch, 56),
    borderRadius: radius.button,
    paddingHorizontal: spacing.xl,
    alignItems: 'center',
    justifyContent: 'center',
  },
  row: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
});
