import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, QrCode, Link as LinkIcon, Send, Copy, Check } from 'lucide-react';
import { invoke } from '@tauri-apps/api/core';

interface PeerPairingProps {
  onClose: () => void;
}

export const PeerPairing: React.FC<PeerPairingProps> = ({ onClose }) => {
  const [ticket, setTicket] = useState<string | null>(null);
  const [remoteTicket, setRemoteTicket] = useState('');
  const [isConnecting, setIsConnecting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    generateTicket();
  }, []);

  const generateTicket = async () => {
    try {
      const t = await invoke<string>('generate_p2p_ticket');
      setTicket(t);
    } catch (err) {
      setError('Failed to generate pairing ticket');
    }
  };

  const handleConnect = async () => {
    if (!remoteTicket) return;
    setIsConnecting(true);
    setError(null);
    try {
      await invoke('connect_peer', { ticket: remoteTicket });
      onClose();
    } catch (err) {
      setError(err as string);
      setIsConnecting(false);
    }
  };

  const copyTicket = () => {
    if (ticket) {
      navigator.clipboard.writeText(ticket);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-6 bg-black/50 backdrop-blur-sm">
      <motion.div 
        initial={{ scale: 0.9, opacity: 0, y: 20 }}
        animate={{ scale: 1, opacity: 1, y: 0 }}
        exit={{ scale: 0.9, opacity: 0, y: 20 }}
        className="bg-[var(--md-sys-color-surface-container-high)] w-full max-w-lg rounded-[2.5rem] p-8 shadow-2xl relative border border-[var(--md-sys-color-outline-variant)]"
      >
        <button 
          onClick={onClose}
          className="absolute top-6 right-6 p-2 rounded-full hover:bg-[var(--md-sys-color-surface-container-highest)] transition-colors"
        >
          <X size={24} />
        </button>

        <h2 className="text-2xl font-semibold mb-6 pr-12">Pair New Device</h2>

        <div className="space-y-8">
          {/* Share Ticket */}
          <div>
            <div className="flex items-center gap-2 mb-4 text-[var(--md-sys-color-primary)]">
              <QrCode size={20} />
              <h3 className="font-medium text-sm uppercase tracking-wider">Your Pairing Ticket</h3>
            </div>
            
            <div className="bg-[var(--md-sys-color-surface)] rounded-2xl p-4 border border-[var(--md-sys-color-outline-variant)] relative group">
              <div className="text-xs font-mono break-all line-clamp-3 text-[var(--md-sys-color-on-surface-variant)]">
                {ticket || 'Generating ticket...'}
              </div>
              <button 
                onClick={copyTicket}
                className="absolute right-2 top-1/2 -translate-y-1/2 p-3 bg-[var(--md-sys-color-primary-container)] text-[var(--md-sys-color-on-primary-container)] rounded-xl opacity-0 group-hover:opacity-100 transition-opacity"
              >
                {copied ? <Check size={18} /> : <Copy size={18} />}
              </button>
            </div>
            <p className="text-[var(--md-sys-color-on-surface-variant)] text-xs mt-3">
              Copy this ticket and enter it on the other device to establish a direct P2P link.
            </p>
          </div>

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-[var(--md-sys-color-outline-variant)]"></div>
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-[var(--md-sys-color-surface-container-high)] px-4 text-[var(--md-sys-color-on-surface-variant)]">or</span>
            </div>
          </div>

          {/* Connect to Peer */}
          <div>
            <div className="flex items-center gap-2 mb-4 text-[var(--md-sys-color-secondary)]">
              <LinkIcon size={20} />
              <h3 className="font-medium text-sm uppercase tracking-wider">Connect to Remote Device</h3>
            </div>
            
            <div className="flex gap-3">
              <input 
                type="text" 
                value={remoteTicket}
                onChange={e => setRemoteTicket(e.target.value)}
                placeholder="Paste remote ticket here..."
                className="flex-1 bg-[var(--md-sys-color-surface)] border border-[var(--md-sys-color-outline-variant)] rounded-2xl px-5 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-[var(--md-sys-color-secondary)] transition-all"
              />
              <button 
                onClick={handleConnect}
                disabled={!remoteTicket || isConnecting}
                className="bg-[var(--md-sys-color-secondary)] text-[var(--md-sys-color-on-secondary)] p-4 rounded-2xl disabled:opacity-50 hover:opacity-90 transition-opacity"
              >
                {isConnecting ? (
                  <div className="w-5 h-5 border-2 border-current border-t-transparent rounded-full animate-spin" />
                ) : (
                  <Send size={20} />
                )}
              </button>
            </div>
            {error && <p className="text-[var(--md-sys-color-error)] text-xs mt-2">{error}</p>}
          </div>
        </div>

        <div className="mt-8 pt-6 border-t border-[var(--md-sys-color-outline-variant)] flex justify-end">
          <button 
            onClick={onClose}
            className="text-[var(--md-sys-color-primary)] font-medium px-6 py-2 rounded-full hover:bg-[var(--md-sys-color-primary-container)]/20 transition-colors"
          >
            Done
          </button>
        </div>
      </motion.div>
    </div>
  );
};
