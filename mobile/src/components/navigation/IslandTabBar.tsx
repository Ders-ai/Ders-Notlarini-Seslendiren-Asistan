import { IconBooks, IconCompass, IconHome, IconUser, IconUsers } from '@tabler/icons-react-native';
import type { BottomTabBarProps } from 'expo-router/js-tabs';
import { Pressable, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { minTouch, spacing, useTheme } from '@/theme';

const ICONS: Record<string, typeof IconHome> = {
  index: IconHome,
  kitaplik: IconBooks,
  akis: IconCompass,
  arkadaslar: IconUsers,
  profil: IconUser,
};

/** Figma'daki ada şeklindeki alt menü: 358 × 72, köşe 36, alttan 20. */
export function IslandTabBar({ state, descriptors, emitter, navigateToTab }: BottomTabBarProps) {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();

  return (
    <View pointerEvents="box-none" style={[styles.wrap, { bottom: Math.max(insets.bottom, spacing.lg) }]}>
      <View style={[styles.island, { backgroundColor: colors.surface, borderColor: colors.border }]}>
        {state.routes.map((route, index) => {
          const focused = state.index === index;
          const { options } = descriptors[route.key];
          const Icon = ICONS[route.name] ?? IconHome;

          const onPress = () => {
            const event = emitter.emit({ type: 'tabPress', target: route.key, canPreventDefault: true });
            if (!focused && !event.defaultPrevented) navigateToTab(route.key);
          };

          return (
            <Pressable
              key={route.key}
              accessibilityRole="tab"
              accessibilityState={{ selected: focused }}
              accessibilityLabel={options.title ?? route.name}
              onPress={onPress}
              onLongPress={() => emitter.emit({ type: 'tabLongPress', target: route.key })}
              style={[styles.item, focused && { backgroundColor: colors.border }]}
            >
              <Icon size={26} strokeWidth={1.75} color={focused ? colors.accent : colors.textSecondary} />
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { position: 'absolute', left: 16, right: 16, alignItems: 'center' },
  island: {
    width: '100%',
    maxWidth: 358,
    height: 72,
    borderRadius: 36,
    borderWidth: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.md,
    shadowColor: '#382B29',
    shadowOpacity: 0.12,
    shadowRadius: 20,
    shadowOffset: { width: 0, height: 8 },
    elevation: 8,
  },
  item: { width: 60, height: minTouch, borderRadius: 20, alignItems: 'center', justifyContent: 'center' },
});
