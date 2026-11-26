import { Tabs } from 'expo-router/js-tabs';

import { IslandTabBar } from '@/components/navigation/IslandTabBar';

export default function TabsLayout() {
  return (
    <Tabs screenOptions={{ headerShown: false }} tabBar={(props) => <IslandTabBar {...props} />}>
      <Tabs.Screen name="index" options={{ title: 'Bugün' }} />
      <Tabs.Screen name="kitaplik" options={{ title: 'Kitaplık' }} />
      <Tabs.Screen name="akis" options={{ title: 'Akış' }} />
      <Tabs.Screen name="arkadaslar" options={{ title: 'Arkadaşlar' }} />
      <Tabs.Screen name="profil" options={{ title: 'Profil' }} />
    </Tabs>
  );
}
