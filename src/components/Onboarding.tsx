import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowRight, FolderOpen, Github, Lock, CheckCircle2, ChevronLeft } from 'lucide-react';
import { open } from '@tauri-apps/plugin-dialog';
import { invoke } from '@tauri-apps/api/core';

interface OnboardingProps {
  onComplete: () => void;
}

type Step = 'welcome' | 'vault' | 'github' | 'encryption';

export const Onboarding: React.FC<OnboardingProps> = ({ onComplete }) => {
  const [step, setStep] = useState<Step>('welcome');
  const [vaultPath, setVaultPath] = useState<string>('');
  const [githubConfig, setGithubConfig] = useState({
    token: '',
    owner: '',
    repo: '',
    branch: 'main',
  });
  const [encryptionKey, setEncryptionKey] = useState('');
  const [isInitializing, setIsInitializing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const steps: Step[] = ['welcome', 'vault', 'github', 'encryption'];
  const currentStepIndex = steps.indexOf(step);

  const handleNext = () => {
    if (step === 'welcome') setStep('vault');
    else if (step === 'vault') setStep('github');
    else if (step === 'github') setStep('encryption');
    else if (step === 'encryption') handleInitialize();
  };

  const handleBack = () => {
    if (step === 'vault') setStep('welcome');
    else if (step === 'github') setStep('vault');
    else if (step === 'encryption') setStep('github');
  };

  const pickDirectory = async () => {
    try {
      const selected = await open({
        directory: true,
        multiple: false,
        title: 'Select your Vault Directory',
      });
      if (selected && typeof selected === 'string') {
        setVaultPath(selected);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleInitialize = async () => {
    setIsInitializing(true);
    setError(null);
    try {
      await invoke('initialize_sync', {
        vaultPath,
        githubConfig: githubConfig.token ? githubConfig : null,
        encryptionKey,
      });
      onComplete();
    } catch (err) {
      setError(err as string);
      setIsInitializing(false);
    }
  };

  const variants = {
    enter: (direction: number) => ({
      x: direction > 0 ? 1000 : -1000,
      opacity: 0
    }),
    center: {
      x: 0,
      opacity: 1
    },
    exit: (direction: number) => ({
      x: direction < 0 ? 1000 : -1000,
      opacity: 0
    })
  };

  const direction = 1; // Always sliding forward for now or implement based on index

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--md-sys-color-surface)] p-6">
      <div className="max-w-md w-full relative overflow-hidden bg-[var(--md-sys-color-surface-container)] rounded-[2.5rem] p-8 shadow-xl border border-[var(--md-sys-color-outline-variant)]">
        
        {/* Progress Dots */}
        <div className="flex justify-center gap-2 mb-8">
          {steps.map((_, i) => (
            <div 
              key={i} 
              className={`h-2 rounded-full transition-all duration-300 ${
                i === currentStepIndex 
                  ? 'w-8 bg-[var(--md-sys-color-primary)]' 
                  : 'w-2 bg-[var(--md-sys-color-outline-variant)]'
              }`}
            />
          ))}
        </div>

        <AnimatePresence mode="wait" custom={direction}>
          <motion.div
            key={step}
            custom={direction}
            variants={variants}
            initial="enter"
            animate="center"
            exit="exit"
            transition={{
              x: { type: "spring", stiffness: 300, damping: 30 },
              opacity: { duration: 0.2 }
            }}
            className="flex flex-col min-h-[400px]"
          >
            {step === 'welcome' && (
              <div className="flex-1 flex flex-col items-center text-center">
                <div className="w-20 h-20 rounded-[2rem] bg-[var(--md-sys-color-primary-container)] flex items-center justify-center text-[var(--md-sys-color-on-primary-container)] mb-6">
                  <CheckCircle2 size={40} />
                </div>
                <h2 className="text-3xl font-semibold mb-4 text-[var(--md-sys-color-on-surface)]">Welcome to OverSync</h2>
                <p className="text-[var(--md-sys-color-on-surface-variant)] mb-8">
                  The ultimate private and fast sync engine for your personal vaults and notes.
                </p>
                <div className="mt-auto w-full">
                  <button 
                    onClick={handleNext}
                    className="w-full bg-[var(--md-sys-color-primary)] text-[var(--md-sys-color-on-primary)] py-4 rounded-full font-medium flex items-center justify-center gap-2 hover:opacity-90 transition-opacity"
                  >
                    Get Started <ArrowRight size={20} />
                  </button>
                </div>
              </div>
            )}

            {step === 'vault' && (
              <div className="flex-1 flex flex-col">
                <h2 className="text-2xl font-semibold mb-2">Local Vault</h2>
                <p className="text-[var(--md-sys-color-on-surface-variant)] mb-8">
                  Choose the directory you want to keep in sync.
                </p>
                
                <div 
                  onClick={pickDirectory}
                  className="flex-1 flex flex-col items-center justify-center border-2 border-dashed border-[var(--md-sys-color-outline)] rounded-[2rem] p-8 cursor-pointer hover:bg-[var(--md-sys-color-surface-container-high)] transition-colors group"
                >
                  <FolderOpen size={48} className="text-[var(--md-sys-color-primary)] mb-4 group-hover:scale-110 transition-transform" />
                  <span className="text-sm font-medium text-center">
                    {vaultPath || 'Click to select directory'}
                  </span>
                </div>

                <div className="mt-8 flex gap-3">
                  <button onClick={handleBack} className="p-4 rounded-full border border-[var(--md-sys-color-outline)] text-[var(--md-sys-color-primary)]">
                    <ChevronLeft size={24} />
                  </button>
                  <button 
                    onClick={handleNext}
                    disabled={!vaultPath}
                    className="flex-1 bg-[var(--md-sys-color-primary)] text-[var(--md-sys-color-on-primary)] py-4 rounded-full font-medium disabled:opacity-50"
                  >
                    Continue
                  </button>
                </div>
              </div>
            )}

            {step === 'github' && (
              <div className="flex-1 flex flex-col">
                <div className="flex items-center gap-3 mb-2">
                  <Github size={24} className="text-[var(--md-sys-color-primary)]" />
                  <h2 className="text-2xl font-semibold">GitHub Sync</h2>
                </div>
                <p className="text-[var(--md-sys-color-on-surface-variant)] mb-6 text-sm">
                  Optional: Sync your encrypted vault to a private GitHub repository.
                </p>
                
                <div className="space-y-4">
                  <Input 
                    label="Personal Access Token" 
                    type="password" 
                    value={githubConfig.token} 
                    onChange={v => setGithubConfig({...githubConfig, token: v})} 
                  />
                  <div className="grid grid-cols-2 gap-4">
                    <Input 
                      label="Owner" 
                      value={githubConfig.owner} 
                      onChange={v => setGithubConfig({...githubConfig, owner: v})} 
                    />
                    <Input 
                      label="Repo Name" 
                      value={githubConfig.repo} 
                      onChange={v => setGithubConfig({...githubConfig, repo: v})} 
                    />
                  </div>
                  <Input 
                    label="Branch" 
                    value={githubConfig.branch} 
                    onChange={v => setGithubConfig({...githubConfig, branch: v})} 
                  />
                </div>

                <div className="mt-auto pt-8 flex gap-3">
                  <button onClick={handleBack} className="p-4 rounded-full border border-[var(--md-sys-color-outline)] text-[var(--md-sys-color-primary)]">
                    <ChevronLeft size={24} />
                  </button>
                  <button 
                    onClick={handleNext}
                    className="flex-1 bg-[var(--md-sys-color-primary)] text-[var(--md-sys-color-on-primary)] py-4 rounded-full font-medium"
                  >
                    {githubConfig.token ? 'Continue' : 'Skip for now'}
                  </button>
                </div>
              </div>
            )}

            {step === 'encryption' && (
              <div className="flex-1 flex flex-col">
                <div className="flex items-center gap-3 mb-2">
                  <Lock size={24} className="text-[var(--md-sys-color-primary)]" />
                  <h2 className="text-2xl font-semibold">Security</h2>
                </div>
                <p className="text-[var(--md-sys-color-on-surface-variant)] mb-8 text-sm">
                  Your files are encrypted before leaving your device. Set a strong master key.
                </p>
                
                <Input 
                  label="Master Encryption Key" 
                  type="password" 
                  value={encryptionKey} 
                  onChange={setEncryptionKey} 
                  placeholder="Minimum 32 characters recommended"
                />

                {error && (
                  <p className="mt-4 text-[var(--md-sys-color-error)] text-sm">{error}</p>
                )}

                <div className="mt-auto pt-8 flex gap-3">
                  <button onClick={handleBack} className="p-4 rounded-full border border-[var(--md-sys-color-outline)] text-[var(--md-sys-color-primary)]">
                    <ChevronLeft size={24} />
                  </button>
                  <button 
                    onClick={handleNext}
                    disabled={!encryptionKey || isInitializing}
                    className="flex-1 bg-[var(--md-sys-color-primary)] text-[var(--md-sys-color-on-primary)] py-4 rounded-full font-medium disabled:opacity-50 flex items-center justify-center"
                  >
                    {isInitializing ? (
                      <div className="w-6 h-6 border-2 border-[var(--md-sys-color-on-primary)] border-t-transparent rounded-full animate-spin" />
                    ) : (
                      'Finish Setup'
                    )}
                  </button>
                </div>
              </div>
            )}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
};

const Input = ({ label, value, onChange, type = 'text', placeholder = '' }: { 
  label: string, 
  value: string, 
  onChange: (v: string) => void, 
  type?: string,
  placeholder?: string
}) => (
  <div className="flex flex-col gap-1.5">
    <label className="text-xs font-medium text-[var(--md-sys-color-on-surface-variant)] ml-4">
      {label}
    </label>
    <input 
      type={type}
      value={value}
      placeholder={placeholder}
      onChange={e => onChange(e.target.value)}
      className="bg-[var(--md-sys-color-surface)] border border-[var(--md-sys-color-outline-variant)] rounded-2xl px-5 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-[var(--md-sys-color-primary)] transition-all"
    />
  </div>
);
