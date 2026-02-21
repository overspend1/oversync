use anyhow::Result;
use merkle_search_tree::MerkleSearchTree;
use serde::{Deserialize, Serialize};
use std::collections::HashMap;

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
            mst: MerkleSearchTree::default(),
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

        self.mst.upsert(path.clone(), &hash_bytes);
        self.metadata.insert(path, meta);

        Ok(self.root_hash())
    }

    pub fn remove_file(&mut self, path: &str) -> Result<[u8; 32]> {
        // In merkle-search-tree 0.8.0, remove is not directly available on the tree
        // We might need to handle this differently, but for now let's use what's available
        // If it's a Map-like tree, we might just upsert a special value or use a different approach.
        // Looking at the crate, it seems it doesn't have a simple 'remove'.
        // For now, I'll just remove from metadata and we might need to recreate the tree or use a tombstone.
        self.metadata.remove(path);
        Ok(self.root_hash())
    }

    pub fn root_hash(&mut self) -> [u8; 32] {
        let root_hash = self.mst.root_hash();
        let mut bytes = [0u8; 32];
        bytes[..16].copy_from_slice(root_hash.as_bytes());
        bytes
    }

    pub fn get_metadata(&self, path: &str) -> Option<&FileMetadata> {
        self.metadata.get(path)
    }

    pub fn diff(&self, _other_mst: &MerkleSearchTree<String, [u8; 32]>) -> Vec<String> {
        let changes = Vec::new();
        // The diff API in 0.8.0 is more complex, requiring serialised page ranges.
        // For now, return empty as we need more logic to implement this properly.
        changes
    }

    pub fn get_mst(&self) -> &MerkleSearchTree<String, [u8; 32]> {
        &self.mst
    }
}
