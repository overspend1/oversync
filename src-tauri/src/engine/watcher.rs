use anyhow::Result;
use notify::{Config, Event, RecursiveMode, Watcher};
use std::path::Path;
use tokio::sync::mpsc;

pub struct VaultWatcher {
    watcher: notify::RecommendedWatcher,
}

impl VaultWatcher {
    pub fn new(path: &Path, tx: mpsc::UnboundedSender<Event>) -> Result<Self> {
        let mut watcher = notify::recommended_watcher(move |res: notify::Result<Event>| {
            if let Ok(event) = res {
                let _ = tx.send(event);
            }
        })?;

        watcher.watch(path, RecursiveMode::Recursive)?;

        Ok(Self { watcher })
    }
}
