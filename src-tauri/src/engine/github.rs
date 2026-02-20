use anyhow::{anyhow, Result};
use octocrab::{models::repos::GitUser, Octocrab};
use serde::{Deserialize, Serialize};
use std::path::Path;
use crate::engine::encryption::Encryptor;
use base64::{engine::general_purpose, Engine as _};

pub struct GitHubStorage {
    client: Octocrab,
    owner: String,
    repo: String,
    branch: String,
    encryptor: Encryptor,
}

#[derive(Debug, Serialize, Deserialize)]
struct EncryptedBlob {
    ciphertext: Vec<u8>,
    nonce: [u8; 24],
}

impl GitHubStorage {
    pub fn new(token: String, owner: String, repo: String, branch: String, encryption_key: &[u8; 32]) -> Result<Self> {
        let client = Octocrab::builder()
            .personal_token(token)
            .build()?;
        
        Ok(Self {
            client,
            owner,
            repo,
            branch,
            encryptor: Encryptor::new(encryption_key),
        })
    }

    pub async fn upload_file(&self, path: &str, content: &[u8]) -> Result<String> {
        let (ciphertext, nonce) = self.encryptor.encrypt(content)?;
        let blob_data = EncryptedBlob { ciphertext, nonce };
        let serialized = serde_json::to_vec(&blob_data)?;
        let encoded = general_purpose::STANDARD.encode(&serialized);

        let blob = self.client
            .repos(&self.owner, &self.repo)
            .git()
            .create_blob(encoded, "base64")
            .send()
            .await?;

        Ok(blob.sha)
    }

    pub async fn download_file(&self, sha: &str) -> Result<Vec<u8>> {
        let blob = self.client
            .repos(&self.owner, &self.repo)
            .git()
            .get_blob(sha)
            .await?;

        let decoded = general_purpose::STANDARD.decode(blob.content.trim().replace("\n", ""))?;
        let encrypted: EncryptedBlob = serde_json::from_slice(&decoded)?;
        
        let plaintext = self.encryptor.decrypt(&encrypted.ciphertext, &encrypted.nonce)?;
        Ok(plaintext)
    }

    pub async fn get_tree(&self, sha: &str) -> Result<octocrab::models::repos::Tree> {
        let tree = self.client
            .repos(&self.owner, &self.repo)
            .git()
            .get_tree(sha)
            .send()
            .await?;
        Ok(tree)
    }

    pub async fn update_state(&self, path: &str, sha: &str, message: &str) -> Result<()> {
        // This is a simplified version of pushing to a branch via Git Data API
        // In a real implementation, you'd create a tree, a commit, and update the ref
        let repo = self.client.repos(&self.owner, &self.repo);
        
        // 1. Get the current commit SHA of the branch
        let branch_ref = repo.git().get_ref(&format!("heads/{}", self.branch)).await?;
        let parent_sha = match branch_ref.object {
            octocrab::models::repos::Object::Commit { sha, .. } => sha,
            octocrab::models::repos::Object::Tag { sha, .. } => sha,
            _ => return Err(anyhow!("Unexpected ref object type")),
        };

        // 2. Create a new tree
        let tree_element = octocrab::params::repos::git::CreateTreeElement {
            path: path.to_string(),
            mode: "100644".to_string(),
            r#type: "blob".to_string(),
            sha: Some(sha.to_string()),
            content: None,
        };

        let tree = repo.git()
            .create_tree(vec![tree_element])
            .base_tree(parent_sha.clone())
            .send()
            .await?;

        // 3. Create a commit
        let commit = repo.git()
            .create_commit(message.to_string(), tree.sha, vec![parent_sha])
            .send()
            .await?;

        // 4. Update the ref
        repo.git()
            .update_ref(&format!("heads/{}", self.branch), commit.sha)
            .send()
            .await?;

        Ok(())
    }
}
