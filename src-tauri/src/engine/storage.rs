use anyhow::{anyhow, Result};
use blake3::Hash;
use merkle_search_tree::{diff::diff, MerkleSearchTree};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::path::{Path, PathBuf};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
pub struct FileMetadata {
    pub path: String,
    pub size: u64,
    pub hash: [u8; 32],
    pub last_modified: u64,
}

pub struct VaultIndexer {
    pub(crate) mst: MerkleSearchTree<String, [u8; 32]>,
    pub(crate) metadata: HashMap<String, FileMetadata>,
}

impl VaultIndexer {
    pub fn new() -> Self {
        Self {
            mst: MerkleSearchTree::new(),
            metadata: HashMap::new(),
        }
    }

    pub fn update_file(
        &mut self,
        path: String,
        content: &[u8],
        last_modified: u64,
    ) -> Result<[u8; 32]> {
        let hash = blake3::hash(content);
        let hash_bytes: [u8; 32] = hash.into();

        let meta = FileMetadata {
            path: path.clone(),
            size: content.len() as u64,
            hash: hash_bytes,
            last_modified,
        };

        self.mst.upsert(path.clone(), hash_bytes);
        self.metadata.insert(path, meta);

        Ok(self.root_hash())
    }

    pub fn remove_file(&mut self, path: &str) -> Result<[u8; 32]> {
        self.mst.remove(path);
        self.metadata.remove(path);
        Ok(self.root_hash())
    }

    pub fn root_hash(&self) -> [u8; 32] {
        self.mst.root_hash().into()
    }

    pub fn get_metadata(&self, path: &str) -> Option<&FileMetadata> {
        self.metadata.get(path)
    }

    pub fn diff(&self, other_mst: &MerkleSearchTree<String, [u8; 32]>) -> Vec<String> {
        let mut changes = Vec::new();
        for change in diff(&self.mst, other_mst) {
            changes.push(change.key().clone());
        }
        changes
    }

    pub fn get_mst(&self) -> &MerkleSearchTree<String, [u8; 32]> {
        &self.mst
    }
}
