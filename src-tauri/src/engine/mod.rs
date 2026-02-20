pub mod p2p;
pub mod watcher;
pub mod github;
pub mod encryption;
pub mod storage;
pub mod sync;

use serde::{Serialize, Deserialize};

pub use p2p::{P2pNode, P2pEvent};
pub use sync::SyncEngine;

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct SyncStatus {
    pub is_syncing: bool,
    pub last_sync: Option<chrono::DateTime<chrono::Utc>>,
    pub peers_connected: usize,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct GithubConfig {
    pub token: String,
    pub owner: String,
    pub repo: String,
    pub branch: String,
}
