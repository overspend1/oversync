use std::path::PathBuf;
use std::sync::Arc;
use anyhow::Result;
use tokio::sync::{mpsc, RwLock};
use notify::Event;
use crate::engine::p2p::{P2pNode, P2pEvent};
use crate::engine::storage::VaultIndexer;
use crate::engine::watcher::VaultWatcher;
use crate::engine::github::GitHubStorage;
use crate::engine::encryption::Encryptor;
use crate::engine::{SyncStatus, GithubConfig};
use chrono::Utc;

pub struct SyncEngine {
    pub p2p: Arc<P2pNode>,
    pub indexer: Arc<RwLock<VaultIndexer>>,
    pub watcher: Option<VaultWatcher>,
    pub github: Option<Arc<GitHubStorage>>,
    pub status: Arc<RwLock<SyncStatus>>,
    pub vault_path: PathBuf,
    pub encryptor: Arc<Encryptor>,
}

impl SyncEngine {
    pub async fn new(
        vault_path: PathBuf,
        p2p_data_dir: PathBuf,
        encryption_key: [u8; 32],
        github_config: Option<GithubConfig>,
    ) -> Result<Arc<Self>> {
        let p2p = Arc::new(P2pNode::new(p2p_data_dir).await?);
        let indexer = Arc::new(RwLock::new(VaultIndexer::new()));
        let encryptor = Arc::new(Encryptor::new(&encryption_key));
        
        let github = if let Some(config) = github_config {
            Some(Arc::new(GitHubStorage::new(config.token, config.owner, config.repo, config.branch, &encryption_key)?))
        } else {
            None
        };

        let status = Arc::new(RwLock::new(SyncStatus {
            is_syncing: false,
            last_sync: None,
            peers_connected: 0,
        }));

        let (tx, mut rx) = mpsc::unbounded_channel();
        let watcher = VaultWatcher::new(&vault_path, tx)?;

        let engine = Arc::new(Self {
            p2p,
            indexer,
            watcher: Some(watcher),
            github,
            status,
            vault_path: vault_path.clone(),
            encryptor,
        });

        let engine_clone = engine.clone();
        tokio::spawn(async move {
            while let Some(event) = rx.recv().await {
                if let Err(e) = engine_clone.handle_watcher_event(event).await {
                    eprintln!("Error handling watcher event: {}", e);
                }
            }
        });

        let engine_clone = engine.clone();
        let mut p2p_rx = engine.p2p.subscribe();
        tokio::spawn(async move {
            while let Ok(event) = p2p_rx.recv().await {
                engine_clone.handle_p2p_event(event).await;
            }
        });

        Ok(engine)
    }

    async fn handle_watcher_event(&self, event: Event) -> Result<()> {
        use notify::event::{EventKind, ModifyKind};

        match event.kind {
            EventKind::Modify(ModifyKind::Data(_)) | EventKind::Create(_) => {
                for path in event.paths {
                    self.process_file_change(path).await?;
                }
            }
            EventKind::Remove(_) => {
                for path in event.paths {
                    self.process_file_removal(path).await?;
                }
            }
            _ => {}
        }

        Ok(())
    }

    async fn process_file_change(&self, path: PathBuf) -> Result<()> {
        let relative_path = path.strip_prefix(&self.vault_path)?
            .to_string_lossy()
            .to_string();

        let content = tokio::fs::read(&path).await?;
        let last_modified = Utc::now().timestamp() as u64;

        // 1. Update Indexer
        let mut indexer = self.indexer.write().await;
        indexer.update_file(relative_path.clone(), &content, last_modified)?;
        let _root_hash = indexer.root_hash();
        drop(indexer);

        // 2. Encrypt file
        let (ciphertext, _nonce) = self.encryptor.encrypt(&content)?;

        // 3. Add to Iroh Blobs and notify peers
        let p2p = self.p2p.clone();
        let ciphertext_clone = ciphertext.clone();
        tokio::spawn(async move {
            if let Err(e) = p2p.add_blob(ciphertext_clone).await {
                eprintln!("Failed to add blob to Iroh: {}", e);
            }
        });

        // 4. Queue to GitHub
        if let Some(github) = &self.github {
            let github = github.clone();
            let rel_path_clone = relative_path.clone();
            tokio::spawn(async move {
                if let Err(e) = github.upload_file(&rel_path_clone, &content).await {
                    eprintln!("GitHub upload failed for {}: {}", rel_path_clone, e);
                } else {
                    println!("GitHub upload successful for {}", rel_path_clone);
                }
            });
        }

        let mut status = self.status.write().await;
        status.last_sync = Some(Utc::now());
        
        Ok(())
    }

    async fn process_file_removal(&self, path: PathBuf) -> Result<()> {
        let relative_path = path.strip_prefix(&self.vault_path)?
            .to_string_lossy()
            .to_string();

        let mut indexer = self.indexer.write().await;
        indexer.remove_file(&relative_path)?;
        
        let mut status = self.status.write().await;
        status.last_sync = Some(Utc::now());

        Ok(())
    }

    async fn handle_p2p_event(&self, event: P2pEvent) {
        match event {
            P2pEvent::PeerConnected(_) => {
                let mut status = self.status.write().await;
                status.peers_connected += 1;
            }
            P2pEvent::PeerDisconnected(_) => {
                let mut status = self.status.write().await;
                status.peers_connected = status.peers_connected.saturating_sub(1);
            }
            _ => {}
        }
    }

    pub async fn get_status(&self) -> SyncStatus {
        self.status.read().await.clone()
    }

    pub async fn get_recent_activity(&self) -> Vec<crate::engine::storage::FileMetadata> {
        let indexer = self.indexer.read().await;
        let mut activity: Vec<_> = indexer.metadata.values().cloned().collect();
        activity.sort_by(|a, b| b.last_modified.cmp(&a.last_modified));
        activity.truncate(10);
        activity
    }
}
