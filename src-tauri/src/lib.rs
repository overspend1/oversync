pub mod engine;

use std::path::PathBuf;
use std::sync::Arc;
use crate::engine::{SyncEngine, GithubConfig, SyncStatus};
use tokio::sync::RwLock;
use tauri::Manager;

pub struct AppState {
    pub sync_engine: RwLock<Option<Arc<SyncEngine>>>,
}

#[tauri::command]
async fn initialize_sync(
    app: tauri::AppHandle,
    state: tauri::State<'_, AppState>,
    vault_path: String,
    github_config: Option<GithubConfig>,
    encryption_key: String,
) -> Result<(), String> {
    let mut key_bytes = [0u8; 32];
    let key_src = encryption_key.as_bytes();
    let len = key_src.len().min(32);
    key_bytes[..len].copy_from_slice(&key_src[..len]);

    let app_data_dir = app.path().app_data_dir().map_err(|e| e.to_string())?;
    let p2p_data_dir = app_data_dir.join("p2p_data");

    let engine = SyncEngine::new(
        PathBuf::from(vault_path),
        p2p_data_dir,
        key_bytes,
        github_config,
    ).await.map_err(|e| e.to_string())?;

    let mut sync_engine = state.sync_engine.write().await;
    *sync_engine = Some(engine);
    
    Ok(())
}

#[tauri::command]
async fn get_sync_status(state: tauri::State<'_, AppState>) -> Result<SyncStatus, String> {
    let engine = state.sync_engine.read().await;
    if let Some(engine) = engine.as_ref() {
        Ok(engine.get_status().await)
    } else {
        Err("Sync engine not initialized".to_string())
    }
}

#[tauri::command]
async fn generate_p2p_ticket(state: tauri::State<'_, AppState>) -> Result<String, String> {
    let engine = state.sync_engine.read().await;
    if let Some(engine) = engine.as_ref() {
        engine.p2p.ticket().await.map_err(|e| e.to_string())
    } else {
        Err("Sync engine not initialized".to_string())
    }
}

#[tauri::command]
async fn connect_peer(state: tauri::State<'_, AppState>, ticket: String) -> Result<(), String> {
    let engine = state.sync_engine.read().await;
    if let Some(engine) = engine.as_ref() {
        engine.p2p.connect(&ticket).await.map_err(|e| e.to_string())
    } else {
        Err("Sync engine not initialized".to_string())
    }
}

#[tauri::command]
async fn get_recent_activity(state: tauri::State<'_, AppState>) -> Result<Vec<crate::engine::storage::FileMetadata>, String> {
    let engine = state.sync_engine.read().await;
    if let Some(engine) = engine.as_ref() {
        Ok(engine.get_recent_activity().await)
    } else {
        Err("Sync engine not initialized".to_string())
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let state = AppState {
        sync_engine: RwLock::new(None),
    };

    let mut builder = tauri::Builder::default();

    #[cfg(target_os = "android")]
    {
        builder = builder.setup(|app| {
            use tauri::Manager;
            let handle = app.handle().clone();
            
            // Start foreground service on Android
            std::thread::spawn(move || {
                let _ = handle.run_on_main_thread(move || {
                    // This is a placeholder for actual Android foreground service initialization
                    // In a real app, you would use a JNI call or a dedicated plugin
                    println!("Initializing Android Foreground Service for Iroh node...");
                });
            });
            Ok(())
        });
    }

    builder
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_os::init())
        .plugin(tauri_plugin_process::init())
        .manage(state)
        .invoke_handler(tauri::generate_handler![
            initialize_sync,
            get_sync_status,
            generate_p2p_ticket,
            connect_peer,
            get_recent_activity
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
