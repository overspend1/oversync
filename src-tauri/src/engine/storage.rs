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

#[cfg(test)]
mod tests {
    use super::VaultIndexer;

    #[test]
    fn root_hash_changes_on_update() {
        let mut indexer = VaultIndexer::new();

        let initial_hash = indexer.root_hash();
        let content_a = b"hello";
        let content_b = b"hello world";

        let hash_after_a = indexer
            .update_file("note.md".to_string(), content_a, 0)
            .expect("update should succeed");

        assert_ne!(initial_hash, hash_after_a);

        let hash_after_b = indexer
            .update_file("note.md".to_string(), content_b, 1)
            .expect("update should succeed");

        assert_ne!(hash_after_a, hash_after_b);
    }

    #[test]
    fn metadata_tracks_updates() {
        let mut indexer = VaultIndexer::new();
        let content = b"sync test";

        indexer
            .update_file("daily.md".to_string(), content, 42)
            .expect("update should succeed");

        let metadata = indexer
            .get_metadata("daily.md")
            .expect("metadata should exist");

        assert_eq!(metadata.size, content.len() as u64);
        assert_eq!(metadata.last_modified, 42);
    }
}
