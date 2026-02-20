import React, { useEffect, useState } from 'react';
import { Cloud, Wifi, Activity, FileCheck, Share2 } from 'lucide-react';
import { motion } from 'framer-motion';
import { invoke } from '@tauri-apps/api/core';
import { PeerPairing } from './PeerPairing';

interface SyncStatus {
  is_syncing: boolean;
  last_sync: string | null;
  peers_connected: number;
}

interface FileMetadata {
  path: string;
  size: number;
  hash: number[];
  last_modified: number;
}

export const Dashboard = () => {
  const [status, setStatus] = useState<SyncStatus | null>(null);
  const [activity, setActivity] = useState<FileMetadata[]>([]);
  const [showPairing, setShowPairing] = useState(false);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5000);
    return () => clearInterval(interval);
  }, []);

  const fetchData = async () => {
    try {
      const [s, a] = await Promise.all([
        invoke<SyncStatus>('get_sync_status'),
        invoke<FileMetadata[]>('get_recent_activity')
      ]);
      setStatus(s);
      setActivity(a);
    } catch (err) {
      console.error('Failed to fetch dashboard data:', err);
    }
  };

  const formatTime = (timestamp: number) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      <header className="flex justify-between items-end">
        <div>
          <h1 className="text-4xl font-semibold tracking-tight text-[var(--md-sys-color-on-surface)]">
            OverSync
          </h1>
          <p className="text-[var(--md-sys-color-on-surface-variant)] mt-2">
            {status?.is_syncing ? 'Synchronization in progress...' : 'Vault is up to date'}
          </p>
        </div>
        <div className="flex gap-4">
          <StatusBadge 
            icon={<Wifi size={16} />} 
            label={status ? `${status.peers_connected} Peers` : 'P2P Offline'} 
            active={(status?.peers_connected ?? 0) > 0} 
          />
          <StatusBadge icon={<Cloud size={16} />} label="GitHub Connected" active={true} />
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <StatCard 
          icon={<Activity size={24} className="text-[var(--md-sys-color-primary)]" />} 
          label="Sync Health" 
          value="100%" 
          sub="All files verified" 
        />
        <StatCard 
          icon={<FileCheck size={24} className="text-[var(--md-sys-color-tertiary)]" />} 
          label="Last Sync" 
          value={status?.last_sync ? new Date(status.last_sync).toLocaleTimeString() : 'Never'} 
          sub={status?.is_syncing ? 'Syncing...' : 'Idle'} 
        />
        <div onClick={() => setShowPairing(true)} className="cursor-pointer group">
          <StatCard 
            icon={<Share2 size={24} className="text-[var(--md-sys-color-secondary)] group-hover:scale-110 transition-transform" />} 
            label="Active Peers" 
            value={status?.peers_connected?.toString() ?? '0'} 
            sub="Click to pair new device" 
          />
        </div>
      </div>

      <section className="bg-[var(--md-sys-color-surface-container-low)] rounded-[2rem] p-8 border border-[var(--md-sys-color-outline-variant)]">
        <h2 className="text-xl font-medium mb-6">Recent Activity</h2>
        <div className="space-y-4">
          {activity.length > 0 ? activity.map((item, i) => (
            <ActivityItem 
              key={i} 
              file={item.path} 
              time={formatTime(item.last_modified)} 
              status="Synced" 
            />
          )) : (
            <p className="text-[var(--md-sys-color-on-surface-variant)] text-center py-8">No recent activity</p>
          )}
        </div>
      </section>

      {showPairing && (
        <PeerPairing onClose={() => setShowPairing(false)} />
      )}
    </div>
  );
};

const StatusBadge = ({ icon, label, active }: { icon: React.ReactNode, label: string, active: boolean }) => (
  <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${active ? 'bg-[var(--md-sys-color-tertiary-container)] text-[var(--md-sys-color-on-tertiary-container)] border-[var(--md-sys-color-tertiary)]' : 'bg-[var(--md-sys-color-error-container)] text-[var(--md-sys-color-on-error-container)] border-[var(--md-sys-color-error)]'}`}>
    {icon}
    {label}
  </div>
);

const StatCard = ({ icon, label, value, sub }: { icon: React.ReactNode, label: string, value: string, sub: string }) => (
  <div className="bg-[var(--md-sys-color-surface-container)] rounded-[1.5rem] p-6 border border-[var(--md-sys-color-outline-variant)] hover:bg-[var(--md-sys-color-surface-container-high)] transition-colors h-full">
    <div className="mb-4">{icon}</div>
    <div className="text-sm font-medium text-[var(--md-sys-color-on-surface-variant)] uppercase tracking-wider">{label}</div>
    <div className="text-2xl font-bold mt-1">{value}</div>
    <div className="text-xs text-[var(--md-sys-color-on-surface-variant)] mt-1">{sub}</div>
  </div>
);

const ActivityItem = ({ file, time, status }: { file: string, time: string, status: string }) => (
  <div className="flex items-center justify-between p-4 bg-[var(--md-sys-color-surface)] rounded-2xl hover:bg-[var(--md-sys-color-surface-container-high)] transition-colors cursor-default border border-transparent hover:border-[var(--md-sys-color-outline-variant)]">
    <div className="flex items-center gap-4 text-left">
      <div className="w-10 h-10 rounded-full bg-[var(--md-sys-color-primary-container)] flex items-center justify-center text-[var(--md-sys-color-on-primary-container)]">
        <FileCheck size={20} />
      </div>
      <div>
        <div className="font-medium text-sm truncate max-w-[200px] md:max-w-md">{file}</div>
        <div className="text-xs text-[var(--md-sys-color-on-surface-variant)]">{status}</div>
      </div>
    </div>
    <div className="text-xs text-[var(--md-sys-color-on-surface-variant)] font-mono">{time}</div>
  </div>
);
