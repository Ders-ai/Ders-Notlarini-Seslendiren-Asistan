import { Pressable, StyleSheet, type PressableProps } from 'react-native';

import { minTouch, radius, spacing, useTheme } from '@/theme';
import { Text } from './Text';

export type ChipProps = Omit<PressableProps, 'children'> & { label: string; active?: boolean };

export function Chip({ label, active, style, ...rest }: ChipProps) {
  const { colors } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: !!active }}
      hitSlop={(minTouch - 36) / 2}
      style={(state) => [
        styles.chip,
        active
          ? { backgroundColor: colors.primary, borderColor: colors.primary }
          : { backgroundColor: colors.surface, borderColor: colors.border },
        typeof style === 'function' ? style(state) : style,
      ]}
      {...rest}
    >
      <Text variant="caption" style={{ color: active ? colors.onPrimary : colors.textSecondary }}>
        {label}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  chip: {
    height: 36,
    paddingHorizontal: spacing.lg,
    borderRadius: radius.chip,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
