import React from 'react';
import { LayoutDashboard, Settings, Share2, FolderSync } from 'lucide-react';

export const NavigationRail = () => {
  return (
    <nav className="w-20 lg:w-24 h-full bg-[var(--md-sys-color-surface-container)] flex flex-col items-center py-8 gap-8 border-r border-[var(--md-sys-color-outline-variant)]">
      <div className="p-3 bg-[var(--md-sys-color-primary-container)] text-[var(--md-sys-color-on-primary-container)] rounded-2xl mb-4">
        <FolderSync size={28} />
      </div>
      
      <NavItem icon={<LayoutDashboard size={24} />} label="Sync" active />
      <NavItem icon={<Share2 size={24} />} label="Peers" />
      <div className="mt-auto">
        <NavItem icon={<Settings size={24} />} label="Settings" />
      </div>
    </nav>
  );
};

const NavItem = ({ icon, label, active = false }: { icon: React.ReactNode, label: string, active?: boolean }) => (
  <button className={`flex flex-col items-center gap-1 group transition-all duration-200`}>
    <div className={`p-2 rounded-full transition-colors duration-200 ${active ? 'bg-[var(--md-sys-color-secondary-container)] text-[var(--md-sys-color-on-secondary-container)]' : 'hover:bg-[var(--md-sys-color-surface-container-highest)] text-[var(--md-sys-color-on-surface-variant)]'}`}>
      {icon}
    </div>
    <span className="text-[10px] font-medium tracking-tight uppercase opacity-70 group-hover:opacity-100">{label}</span>
  </button>
);
