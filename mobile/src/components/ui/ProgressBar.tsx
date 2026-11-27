import { StyleSheet, View } from 'react-native';

import { useTheme } from '@/theme';

export function ProgressBar({ value, height = 6 }: { value: number; height?: number }) {
  const { colors } = useTheme();
  const pct = Math.max(0, Math.min(1, value)) * 100;
  return (
    <View
      accessibilityRole="progressbar"
      accessibilityValue={{ min: 0, max: 100, now: Math.round(pct) }}
      style={[styles.track, { height, borderRadius: height / 2, backgroundColor: colors.border }]}
    >
      <View style={{ width: `${pct}%`, height, borderRadius: height / 2, backgroundColor: colors.accent }} />
    </View>
  );
}

const styles = StyleSheet.create({ track: { width: '100%', overflow: 'hidden' } });
