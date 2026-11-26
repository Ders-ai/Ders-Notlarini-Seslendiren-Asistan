import type { ReactNode } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, View, type ViewStyle } from 'react-native';
import { SafeAreaView, type Edge } from 'react-native-safe-area-context';

import { spacing, useTheme } from '@/theme';

export type ScreenProps = {
  children: ReactNode;
  scroll?: boolean;
  /** Alt menü (ada) olan sekmelerde içerik menünün altında kalmasın diye boşluk bırakır. */
  withTabBar?: boolean;
  edges?: Edge[];
  contentStyle?: ViewStyle;
};

export const TAB_BAR_SPACE = 112;

export function Screen({ children, scroll = true, withTabBar, edges = ['top', 'left', 'right'], contentStyle }: ScreenProps) {
  const { colors } = useTheme();
  const content = [styles.content, withTabBar && { paddingBottom: TAB_BAR_SPACE }, contentStyle];

  return (
    <SafeAreaView edges={edges} style={[styles.flex, { backgroundColor: colors.background }]}>
      <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        {scroll ? (
          <ScrollView contentContainerStyle={content} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
            {children}
          </ScrollView>
        ) : (
          <View style={[styles.flex, content]}>{children}</View>
        )}
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  content: { paddingHorizontal: spacing.xl, paddingTop: spacing.md, gap: spacing.lg },
});
