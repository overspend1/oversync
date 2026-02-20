pub mod p2p;
pub mod watcher;
pub mod github;
pub mod encryption;
pub mod storage;

use serde::{Serialize, Deserialize};

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct SyncStatus {
    pub is_syncing: bool,
    pub last_sync: Option<chrono::DateTime<chrono::Utc>>,
    pub peers_connected: usize,
}
