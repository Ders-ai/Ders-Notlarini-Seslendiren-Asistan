import { StyleSheet, View } from 'react-native';

import { spacing } from '@/theme';
import { Card } from './Card';
import { Screen } from './Screen';
import { Text } from './Text';

/** Henüz kodlanmamış sekmeler için geçici ekran. */
export function Placeholder({ title, screen }: { title: string; screen: string }) {
  return (
    <Screen withTabBar>
      <Text variant="h1">{title}</Text>
      <Card>
        <View style={styles.body}>
          <Text variant="label">Bu ekran sırada</Text>
          <Text variant="caption" tone="secondary">Figma: {screen}</Text>
        </View>
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({ body: { gap: spacing.xs } });
