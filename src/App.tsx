import React, { useEffect, useState } from 'react';
import { createM3Theme, defaultSeedColor } from './theme/m3';
import { NavigationRail } from './components/NavigationRail';
import { Dashboard } from './components/Dashboard';

function App() {
  const [isDark] = useState(window.matchMedia('(prefers-color-scheme: dark)').matches);

  useEffect(() => {
    createM3Theme(defaultSeedColor, isDark);
  }, [isDark]);

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
