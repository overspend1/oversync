use std::path::PathBuf;
use std::sync::Arc;
use anyhow::Result;
use iroh::node::Node;
use iroh::net::key::SecretKey;
use iroh::base::ticket::NodeTicket;
use iroh::net::NodeId;
use iroh::blobs::store::fs::Store;
use iroh::blobs::protocol::ALPN;
use serde::{Serialize, Deserialize};
use tokio::sync::{broadcast, Mutex};
use tokio::fs;
use tokio_stream::StreamExt;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum P2pEvent {
    PeerConnected(String),
    PeerDisconnected(String),
    SyncStarted(String),
    SyncFinished(String),
    SyncFailed { peer: String, error: String },
}

pub struct P2pNode {
    node: Node<Store>,
    _secret_key: SecretKey,
    event_tx: broadcast::Sender<P2pEvent>,
    active_peers: Arc<Mutex<Vec<NodeId>>>,
}

impl P2pNode {
    pub async fn new(data_dir: PathBuf) -> Result<Self> {
        if !data_dir.exists() {
            fs::create_dir_all(&data_dir).await?;
        }

        let secret_key_path = data_dir.join("secret_key");
        let secret_key = if secret_key_path.exists() {
            let bytes = fs::read(&secret_key_path).await?;
            SecretKey::from_bytes(&bytes.try_into().map_err(|_| anyhow::anyhow!("Invalid secret key length"))?)
        } else {
            let sk = SecretKey::generate();
            fs::write(&secret_key_path, sk.to_bytes()).await?;
            sk
        };

        let node = Node::persistent(data_dir.join("iroh_data"))
            .await?
            .secret_key(secret_key.clone())
            .spawn()
            .await?;

        let (event_tx, _) = broadcast::channel(100);
        let active_peers = Arc::new(Mutex::new(Vec::new()));

        let node_clone = node.clone();

        // Spawn connection monitor
        tokio::spawn(async move {
            let mut endpoint_events = node_clone.endpoint().watch_home_relay();
            while let Some(_) = endpoint_events.next().await {
                // Simplified connection monitoring
            }
        });

        Ok(Self {
            node,
            _secret_key: secret_key,
            event_tx,
            active_peers,
        })
    }

    pub async fn ticket(&self) -> Result<String> {
        let addr = self.node.endpoint().node_addr().await?;
        let ticket = NodeTicket::new(addr)?;
        Ok(ticket.to_string())
    }

    pub async fn connect(&self, ticket_str: &str) -> Result<()> {
        let ticket = ticket_str.parse::<NodeTicket>()
            .map_err(|_| anyhow::anyhow!("Invalid ticket format"))?;
        
        self.node.endpoint().connect(ticket.node_addr().clone(), ALPN).await?;
        
        let peer_id = ticket.node_addr().node_id;
        let mut peers = self.active_peers.lock().await;
        if !peers.contains(&peer_id) {
            peers.push(peer_id);
            let _ = self.event_tx.send(P2pEvent::PeerConnected(peer_id.to_string()));
        }

        Ok(())
    }

    pub async fn sync_blob(&self, peer_id: NodeId, hash: iroh::blobs::Hash) -> Result<()> {
        let _ = self.event_tx.send(P2pEvent::SyncStarted(peer_id.to_string()));
        
        let client = self.node.blobs();
        let download = client.download(hash, peer_id.into()).await?;
        
        match download.await {
            Ok(_) => {
                let _ = self.event_tx.send(P2pEvent::SyncFinished(peer_id.to_string()));
                Ok(())
            }
            Err(e) => {
                let _ = self.event_tx.send(P2pEvent::SyncFailed {
                    peer: peer_id.to_string(),
                    error: e.to_string(),
                });
                Err(e.into())
            }
        }
    }

    pub async fn add_blob(&self, data: Vec<u8>) -> Result<iroh::blobs::Hash> {
        let client = self.node.blobs();
        let hash = client.add_bytes(data).await?;
        Ok(hash.hash)
    }

    pub fn subscribe(&self) -> broadcast::Receiver<P2pEvent> {
        self.event_tx.subscribe()
    }

    pub async fn node_id(&self) -> NodeId {
        self.node.node_id()
    }
}
