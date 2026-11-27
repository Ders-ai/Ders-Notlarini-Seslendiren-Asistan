import { StyleSheet, View, type ViewProps } from 'react-native';

import { radius, spacing, useTheme } from '@/theme';

export type CardProps = ViewProps & { selected?: boolean; padded?: boolean };

export function Card({ selected, padded = true, style, ...rest }: CardProps) {
  const { colors } = useTheme();
  return (
    <View
      style={[
        styles.card,
        padded && styles.padded,
        { backgroundColor: colors.surface, borderColor: selected ? colors.accent : colors.border },
        style,
      ]}
      {...rest}
    />
  );
}

const styles = StyleSheet.create({
  card: { borderRadius: radius.card, borderWidth: 1 },
  padded: { padding: spacing.lg },
});
