use anyhow::{anyhow, Result};
use octocrab::Octocrab;
use serde::{Deserialize, Serialize};
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

        let content_response = self.client
            .repos(&self.owner, &self.repo)
            .create_file(path, "Upload file", encoded)
            .branch(&self.branch)
            .send()
            .await?;

        Ok(content_response.content.sha)
    }

    pub async fn download_file(&self, sha: &str) -> Result<Vec<u8>> {
        let blob = self.client
            .repos(&self.owner, &self.repo)
            .get_content()
            .path(sha)
            .send()
            .await?;

        let item = blob.items.first().ok_or_else(|| anyhow!("No items found"))?;
        let decoded = general_purpose::STANDARD.decode(item.content.as_ref().ok_or_else(|| anyhow!("No content in item"))?.trim().replace("\n", ""))?;
        let encrypted: EncryptedBlob = serde_json::from_slice(&decoded)?;
        
        let plaintext = self.encryptor.decrypt(&encrypted.ciphertext, &encrypted.nonce)?;
        Ok(plaintext)
    }

    pub async fn get_tree(&self, sha: &str) -> Result<octocrab::models::commits::Tree> {
        let tree = self.client
            .get(format!("/repos/{}/{}/git/trees/{}", self.owner, self.repo, sha), None::<&()>)
            .await?;
        Ok(tree)
    }

    pub async fn update_state(&self, path: &str, sha: &str, message: &str) -> Result<()> {
        let repo = self.client.repos(&self.owner, &self.repo);
        
        let branch_ref = repo.get_ref(&octocrab::params::repos::Reference::Branch(self.branch.clone())).await?;
        let parent_sha = match branch_ref.object {
            octocrab::models::repos::Object::Commit { sha, .. } => sha,
            octocrab::models::repos::Object::Tag { sha, .. } => sha,
            _ => return Err(anyhow!("Unexpected ref object type")),
        };

        // Create a new tree
        let tree: octocrab::models::commits::Tree = self.client
            .post(
                format!("/repos/{}/{}/git/trees", self.owner, self.repo),
                Some(&serde_json::json!({
                    "base_tree": parent_sha,
                    "tree": [{
                        "path": path,
                        "mode": "100644",
                        "type": "blob",
                        "sha": sha,
                    }]
                })),
            )
            .await?;

        // Create a commit
        let commit: octocrab::models::commits::GitCommitObject = self.client
            .post(
                format!("/repos/{}/{}/git/commits", self.owner, self.repo),
                Some(&serde_json::json!({
                    "message": message,
                    "tree": tree.sha,
                    "parents": [parent_sha]
                })),
            )
            .await?;

        // Update the ref
        let _ : serde_json::Value = self.client
            .patch(
                format!("/repos/{}/{}/git/refs/heads/{}", self.owner, self.repo, self.branch),
                Some(&serde_json::json!({
                    "sha": commit.sha,
                    "force": false
                })),
            )
            .await?;

        Ok(())
    }
}
