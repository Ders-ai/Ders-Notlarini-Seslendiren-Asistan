import { useState, type ReactNode } from 'react';
import { StyleSheet, TextInput, View, type TextInputProps } from 'react-native';

import { fonts, radius, spacing, useTheme } from '@/theme';
import { Text } from './Text';

export type InputProps = TextInputProps & {
  label?: string;
  icon?: ReactNode;
  right?: ReactNode;
  error?: string;
};

export function Input({ label, icon, right, error, style, onFocus, onBlur, ...rest }: InputProps) {
  const { colors } = useTheme();
  const [focused, setFocused] = useState(false);
  const borderColor = error || focused ? colors.accent : colors.border;

  return (
    <View style={styles.wrap}>
      {label ? (
        <Text variant="caption" tone="secondary" style={styles.label}>
          {label}
        </Text>
      ) : null}
      <View style={[styles.field, { backgroundColor: colors.surface, borderColor }]}>
        {icon}
        <TextInput
          placeholderTextColor={colors.textSecondary}
          style={[styles.input, { color: colors.text }, style]}
          onFocus={(e) => {
            setFocused(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            setFocused(false);
            onBlur?.(e);
          }}
          accessibilityLabel={label}
          {...rest}
        />
        {right}
      </View>
      {error ? (
        <Text variant="caption" tone="accent" style={styles.error} accessibilityLiveRegion="polite">
          {error}
        </Text>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { gap: spacing.xs },
  label: { marginLeft: 2 },
  field: {
    minHeight: 56,
    borderRadius: radius.input,
    borderWidth: 1,
    paddingHorizontal: spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  input: { flex: 1, fontFamily: fonts.regular, fontSize: 16, paddingVertical: spacing.md },
  error: { marginLeft: 2 },
});
