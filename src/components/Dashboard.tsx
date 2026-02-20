import React from 'react';
import { Cloud, Wifi, Activity, FileCheck } from 'lucide-react';
import { motion } from 'framer-motion';

export const Dashboard = () => {
  return (
    <div className="max-w-6xl mx-auto space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-[var(--md-sys-color-on-surface)]">
            OverSync
          </h1>
          <p className="text-[var(--md-sys-color-on-surface-variant)] mt-2">
            Instant vault synchronization active
          </p>
        </div>
        <div className="flex gap-4">
          <StatusBadge icon={<Wifi size={16} />} label="P2P Online" active />
          <StatusBadge icon={<Cloud size={16} />} label="GitHub Connected" active />
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <StatCard 
          icon={<Activity size={24} className="text-[var(--md-sys-color-primary)]" />} 
          label="Sync Health" 
          value="100%" 
          sub="All files synced" 
        />
        <StatCard 
          icon={<FileCheck size={24} className="text-[var(--md-sys-color-tertiary)]" />} 
          label="Last Update" 
          value="Just now" 
          sub="3 files changed" 
        />
        <StatCard 
          icon={<Share2 size={24} className="text-[var(--md-sys-color-secondary)]" />} 
          label="Active Peers" 
          value="2" 
          sub="Laptop, Android" 
        />
      </div>

      <section className="bg-[var(--md-sys-color-surface-container-low)] rounded-[2rem] p-8 border border-[var(--md-sys-color-outline-variant)]">
        <h2 className="text-xl font-medium mb-6">Recent Activity</h2>
        <div className="space-y-4">
          <ActivityItem file="Projects/Obsidian/Roadmap.md" time="2 mins ago" status="Synced" />
          <ActivityItem file="Archive/OldNotes.zip" time="15 mins ago" status="Encrypted & Uploaded" />
          <ActivityItem file="DailyNotes/2026-02-21.md" time="1 hour ago" status="P2P Sync" />
        </div>
      </section>
    </div>
  );
};

const StatusBadge = ({ icon, label, active }: { icon: React.ReactNode, label: string, active: boolean }) => (
  <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border ${active ? 'bg-[var(--md-sys-color-tertiary-container)] text-[var(--md-sys-color-on-tertiary-container)] border-[var(--md-sys-color-tertiary)]' : 'bg-[var(--md-sys-color-error-container)] text-[var(--md-sys-color-on-error-container)] border-[var(--md-sys-color-error)]'}`}>
    {icon}
    {label}
  </div>
);

const StatCard = ({ icon, label, value, sub }: { icon: React.ReactNode, label: string, value: string, sub: string }) => (
  <div className="bg-[var(--md-sys-color-surface-container)] rounded-[1.5rem] p-6 border border-[var(--md-sys-color-outline-variant)]">
    <div className="mb-4">{icon}</div>
    <div className="text-sm font-medium text-[var(--md-sys-color-on-surface-variant)] uppercase tracking-wider">{label}</div>
    <div className="text-2xl font-bold mt-1">{value}</div>
    <div className="text-xs text-[var(--md-sys-color-on-surface-variant)] mt-1">{sub}</div>
  </div>
);

const ActivityItem = ({ file, time, status }: { file: string, time: string, status: string }) => (
  <div className="flex items-center justify-between p-4 bg-[var(--md-sys-color-surface)] rounded-2xl hover:bg-[var(--md-sys-color-surface-container-high)] transition-colors cursor-default">
    <div className="flex items-center gap-4">
      <div className="w-10 h-10 rounded-full bg-[var(--md-sys-color-primary-container)] flex items-center justify-center text-[var(--md-sys-color-on-primary-container)]">
        <FileCheck size={20} />
      </div>
      <div>
        <div className="font-medium text-sm">{file}</div>
        <div className="text-xs text-[var(--md-sys-color-on-surface-variant)]">{status}</div>
      </div>
    </div>
    <div className="text-xs text-[var(--md-sys-color-on-surface-variant)] font-mono">{time}</div>
  </div>
);

import { Share2 } from 'lucide-react';
