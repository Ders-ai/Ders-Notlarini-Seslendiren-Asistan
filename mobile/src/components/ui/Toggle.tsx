import { useEffect } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import Animated, { useAnimatedStyle, useSharedValue, withTiming } from 'react-native-reanimated';

import { minTouch, useTheme } from '@/theme';

export type ToggleProps = {
  value: boolean;
  onValueChange: (value: boolean) => void;
  accessibilityLabel: string;
  disabled?: boolean;
};

const W = 50;
const H = 30;
const KNOB = 24;

export function Toggle({ value, onValueChange, accessibilityLabel, disabled }: ToggleProps) {
  const { colors } = useTheme();
  const x = useSharedValue(value ? W - KNOB - 3 : 3);

  useEffect(() => {
    x.value = withTiming(value ? W - KNOB - 3 : 3, { duration: 160 });
  }, [value, x]);

  const knob = useAnimatedStyle(() => ({ transform: [{ translateX: x.value }] }));

  return (
    <Pressable
      accessibilityRole="switch"
      accessibilityLabel={accessibilityLabel}
      accessibilityState={{ checked: value, disabled }}
      disabled={disabled}
      hitSlop={(minTouch - H) / 2}
      onPress={() => onValueChange(!value)}
      style={[styles.track, { backgroundColor: value ? colors.accent : colors.border, opacity: disabled ? 0.5 : 1 }]}
    >
      <Animated.View style={[styles.knob, knob]} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  track: { width: W, height: H, borderRadius: H / 2, justifyContent: 'center' },
  knob: {
    width: KNOB,
    height: KNOB,
    borderRadius: KNOB / 2,
    backgroundColor: '#FFFFFF',
    shadowColor: '#382B29',
    shadowOpacity: 0.18,
    shadowRadius: 2,
    shadowOffset: { width: 0, height: 1 },
    elevation: 2,
  },
});
