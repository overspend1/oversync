# OverSync 🚀

**OverSync** is a high-performance, P2P-first Obsidian vault synchronization application with **Material You (Material Design 3)** dynamic theming. It provides a lightning-fast, secure, and private way to sync your notes across **Windows, Linux, and Android**.

[![GitHub Release](https://img.shields.io/github/v/release/overspend1/oversync?style=flat-square)](https://github.com/overspend1/oversync/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

---

## ✨ Key Features

- **⚡ P2P Direct Sync:** Powered by **Iroh**, OverSync creates direct encrypted connections between your devices, bypassing the cloud for maximum speed and privacy.
- **🛡️ Zero-Knowledge Cloud:** Use a private **GitHub repository** as a backup. Files are encrypted locally using **XChaCha20Poly1305** before being uploaded. GitHub only sees encrypted blobs.
- **🎨 Material You (M3):** A beautiful, modern interface that automatically extracts accent colors from your system (Android Monet / Windows Accent).
- **⏱️ Instant Sync:** A Rust-based file watcher monitors your vault and syncs changes the moment you save.
- **🌲 Merkle Search Trees (MST):** High-performance state indexing for instant delta calculation between devices.
- **📱 Android Optimized:** Includes foreground service support to maintain sync stability even when the app is minimized.

---

## 🛠️ Tech Stack

- **Framework:** [Tauri v2](https://v2.tauri.app/)
- **Backend (Rust):** `iroh`, `notify`, `merkle-search-tree`, `octocrab`, `chacha20poly1305`
- **Frontend (React/TS):** `Tailwind CSS`, `Framer Motion`, `@material/material-color-utilities`
- **Networking:** [Iroh](https://iroh.computer/) (QUIC-based P2P)

---

## 🚀 Getting Started

### Prerequisites

- [Rust](https://www.rust-lang.org/tools/install)
- [Node.js](https://nodejs.org/)
- [Tauri Prerequisites](https://v2.tauri.app/start/prerequisites/)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/overspend1/oversync.git
   cd oversync
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Run in development mode:**
   ```bash
   npm run tauri dev
   ```

---

## 🔒 Security & Privacy

- **Your Data, Your Keys:** All encryption/decryption happens on-device. OverSync never transmits your master key or plaintext notes.
- **Open Source:** Fully auditable code for the privacy-conscious note-taker.
- **P2P Discovery:** Devices pair using one-time Iroh tickets (QR codes/Keys), ensuring only authorized devices can join your sync pool.

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

---

## 🙌 Credits

Built with ❤️ by [Kilo Agent](https://github.com/overspend1) for [overspend.cloud](https://overspend.cloud).
Special thanks to the **Tauri** and **Iroh** teams for their incredible tools.
