use sqlx::{PgPool, FromRow};
use anyhow::Result;
use uuid::Uuid;
use chrono::{DateTime, Utc};
use serde::{Serialize, Deserialize};

#[derive(Debug, Serialize, Deserialize, FromRow)]
pub struct DeviceInfo {
    pub device_id: Uuid,
    pub name: String,
    pub iroh_ticket: String,
    pub last_seen: DateTime<Utc>,
}

pub struct NeonRelay {
    pool: PgPool,
}

impl NeonRelay {
    pub async fn new(database_url: &str) -> Result<Self> {
        let pool = PgPool::connect(database_url).await?;
        Ok(Self { pool })
    }

    pub async fn register_device(&self, id: Uuid, name: &str, ticket: &str) -> Result<()> {
        sqlx::query(
            "INSERT INTO devices (device_id, name, iroh_ticket, last_seen)
             VALUES ($1, $2, $3, NOW())
             ON CONFLICT (device_id) DO UPDATE 
             SET iroh_ticket = $3, last_seen = NOW()"
        )
        .bind(id)
        .bind(name)
        .bind(ticket)
        .execute(&self.pool)
        .await?;
        Ok(())
    }

    pub async fn get_active_peers(&self) -> Result<Vec<DeviceInfo>> {
        let devices = sqlx::query_as::<_, DeviceInfo>(
            "SELECT device_id, name, iroh_ticket, last_seen FROM devices 
             WHERE last_seen > NOW() - INTERVAL '5 minutes'"
        )
        .fetch_all(&self.pool)
        .await?;
        Ok(devices)
    }

    pub async fn update_vault_root(&self, vault_id: &str, root_hash: &str) -> Result<()> {
        sqlx::query(
            "INSERT INTO vault_state (vault_id, root_hash, updated_at)
             VALUES ($1, $2, NOW())
             ON CONFLICT (vault_id) DO UPDATE SET root_hash = $2, updated_at = NOW()"
        )
        .bind(vault_id)
        .bind(root_hash)
        .execute(&self.pool)
        .await?;
        Ok(())
    }
}
