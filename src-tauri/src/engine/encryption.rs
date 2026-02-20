use anyhow::{anyhow, Result};
use chacha20poly1305::{
    aead::{Aead, KeyInit, Payload},
    XChaCha20Poly1305, XNonce,
};
use rand::{rngs::OsRng, RngCore};

pub struct Encryptor {
    cipher: XChaCha20Poly1305,
}

impl Encryptor {
    pub fn new(key: &[u8; 32]) -> Self {
        let cipher = XChaCha20Poly1305::new(key.into());
        Self { cipher }
    }

    pub fn encrypt(&self, data: &[u8]) -> Result<(Vec<u8>, [u8; 24])> {
        let mut nonce_bytes = [0u8; 24];
        OsRng.fill_bytes(&mut nonce_bytes);
        let nonce = XNonce::from_slice(&nonce_bytes);

        let ciphertext = self
            .cipher
            .encrypt(nonce, data)
            .map_err(|e| anyhow!("encryption failure: {}", e))?;

        Ok((ciphertext, nonce_bytes))
    }

    pub fn decrypt(&self, ciphertext: &[u8], nonce_bytes: &[u8; 24]) -> Result<Vec<u8>> {
        let nonce = XNonce::from_slice(nonce_bytes);

        let plaintext = self
            .cipher
            .decrypt(nonce, ciphertext)
            .map_err(|e| anyhow!("decryption failure: {}", e))?;

        Ok(plaintext)
    }
}
