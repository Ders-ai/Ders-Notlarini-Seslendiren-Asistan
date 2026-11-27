import { IconBell, IconFlame, IconPlayerPlayFilled, IconPlus, IconStack2, IconTarget } from '@tabler/icons-react-native';
import { Pressable, StyleSheet, View } from 'react-native';

import { Card, IconBox, ProgressBar, Screen, Text } from '@/components/ui';
import { palette, radius, spacing, useTheme } from '@/theme';

/** 2.1 · Bugün (ana ekran) — şimdilik örnek verilerle. */
export default function TodayScreen() {
  const { colors } = useTheme();

  return (
    <Screen withTabBar>
      <View style={styles.header}>
        <View style={[styles.avatar, { backgroundColor: colors.border }]}>
          <Text variant="label" tone="accent">DE</Text>
        </View>
        <View style={{ flex: 1 }}>
          <Text variant="caption" tone="secondary">Merhaba Dilara</Text>
          <Text variant="h3">Pazar, 1 Kasım</Text>
        </View>
        <Pressable accessibilityRole="button" accessibilityLabel="Bildirimler" style={[styles.circle, { backgroundColor: colors.surface, borderColor: colors.border }]}>
          <IconBell size={22} strokeWidth={1.75} color={colors.text} />
        </Pressable>
      </View>

      <View style={[styles.continue, { backgroundColor: palette.ana }]}>
        <Text variant="overline" style={{ color: palette.vurgu }}>DEVAM ET</Text>
        <Text variant="h2" style={{ color: palette.zemin }}>İkili Arama Ağaçları</Text>
        <Text variant="caption" style={{ color: palette.vurgu }}>Veri Yapıları · 7 dk kaldı</Text>
        <View style={styles.continueRow}>
          <View style={{ flex: 1 }}>
            <ProgressBar value={0.62} />
          </View>
          <Pressable accessibilityRole="button" accessibilityLabel="Dinlemeye devam et" style={[styles.play, { backgroundColor: palette.nar }]}>
            <IconPlayerPlayFilled size={22} color={palette.beyaz} />
          </Pressable>
        </View>
      </View>

      <View style={styles.grid}>
        <Card style={styles.tile}>
          <IconFlame size={22} strokeWidth={1.75} color={colors.accent} />
          <Text variant="h3">5 gün</Text>
          <Text variant="caption" tone="secondary">çalışma serisi</Text>
        </Card>
        <Card style={styles.tile}>
          <IconStack2 size={22} strokeWidth={1.75} color={colors.text} />
          <Text variant="h3">12 kart</Text>
          <Text variant="caption" tone="secondary">bugün tekrar</Text>
        </Card>
        <Card style={styles.tile}>
          <IconTarget size={22} strokeWidth={1.75} color={colors.text} />
          <Text variant="h3">%80</Text>
          <Text variant="caption" tone="secondary">son test</Text>
        </Card>
        <Pressable accessibilityRole="button" style={[styles.tile, styles.upload, { backgroundColor: colors.border }]}>
          <IconBox tone="muted" size={40}>
            <IconPlus size={22} color={colors.text} />
          </IconBox>
          <Text variant="label">Not yükle</Text>
        </Pressable>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  avatar: { width: 46, height: 46, borderRadius: 23, alignItems: 'center', justifyContent: 'center' },
  circle: { width: 46, height: 46, borderRadius: 23, borderWidth: 1, alignItems: 'center', justifyContent: 'center' },
  continue: { borderRadius: radius.card, padding: spacing.xl, gap: spacing.xs },
  continueRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.lg, marginTop: spacing.md },
  play: { width: 52, height: 52, borderRadius: 26, alignItems: 'center', justifyContent: 'center' },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.md },
  tile: { width: '48%', flexGrow: 1, minHeight: 132, gap: spacing.xs, justifyContent: 'flex-end' },
  upload: { borderRadius: radius.card, alignItems: 'center', justifyContent: 'center', padding: spacing.lg },
});
