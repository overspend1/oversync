import { useEffect, useState } from 'react';
import { createM3Theme, defaultSeedColor } from './theme/m3';
import { NavigationRail } from './components/NavigationRail';
import { Dashboard } from './components/Dashboard';
import { Onboarding } from './components/Onboarding';
import { invoke } from '@tauri-apps/api/core';

function App() {
  const [isDark] = useState(window.matchMedia('(prefers-color-scheme: dark)').matches);
  const [isInitialized, setIsInitialized] = useState<boolean | null>(null);

  useEffect(() => {
    createM3Theme(defaultSeedColor, isDark);
  }, [isDark]);

  useEffect(() => {
    checkInitialization();
  }, []);

  const checkInitialization = async () => {
    try {
      await invoke('get_sync_status');
      setIsInitialized(true);
    } catch (err) {
      setIsInitialized(false);
    }
  };

  if (isInitialized === null) {
    return (
      <div className="flex h-screen w-screen bg-[var(--md-sys-color-surface)] items-center justify-center">
        <div className="w-12 h-12 border-4 border-[var(--md-sys-color-primary)] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!isInitialized) {
    return <Onboarding onComplete={() => setIsInitialized(true)} />;
  }

  return (
    <div className="flex h-screen w-screen bg-[var(--md-sys-color-surface)] text-[var(--md-sys-color-on-surface)] overflow-hidden">
      <NavigationRail />
      <main className="flex-1 overflow-y-auto p-6 transition-colors duration-300">
        <Dashboard />
      </main>
    </div>
  );
}

export default App;
