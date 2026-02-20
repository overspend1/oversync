use iroh::node::Node;
use anyhow::Result;

pub struct P2pNode {
    node: Node,
}

impl P2pNode {
    pub async fn new() -> Result<Self> {
        let node = Node::memory().spawn().await?;
        Ok(Self { node })
    }

    pub fn ticket(&self) -> String {
        // In a real implementation, this would return a join ticket
        "iroh-ticket-placeholder".to_string()
    }
}
