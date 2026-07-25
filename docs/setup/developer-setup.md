# TruthGate — Local Dev & Test Environment Setup

Welcome to the TruthGate playground! This guide gets you from zero to happily testing sites and the TruthGate GUI locally with IPFS. It’s ordered for the smoothest path (install → configure → verify → run → troubleshoot).

> **Quick vibe-check:** You’ll test in **two browser contexts**: one **with IPFS** (Companion enabled) and one **without** (plain browser). This mirrors how real users and bots will hit your content.

---

## 0) Prerequisites

- A modern desktop OS (Windows/macOS/Linux)
    
- Git & a code editor (VS Code, Rider, etc.)
    
- **For Linux:** ensure you can run the `ipfs` CLI (details below)
    

---

## 1) Install IPFS Desktop

Follow the official IPFS Desktop install guide:

- docs: [https://docs.ipfs.tech/install/ipfs-desktop/](https://docs.ipfs.tech/install/ipfs-desktop/)
    

**Windows users:** this is typically all you need.  

---
### Setting up IPFS Desktop + CLI on Linux

On Linux, you’ll want both the **IPFS Desktop app** (GUI) and the **`ipfs` CLI** available in your terminal.  
This ensures you can run commands like `ipfs --version` and have them talk to the same local node that the Desktop app manages.

---

### 1. Install IPFS Desktop

> These instructions utilize Ubuntu. For other Linux operating systems, you'll need to adapt the commands, but the process is the same.

Download the latest `.deb` release from the [official GitHub page](https://github.com/ipfs/ipfs-desktop/releases/).  
For example, version **0.45.0**:

```bash
wget https://github.com/ipfs/ipfs-desktop/releases/download/v0.45.0/ipfs-desktop-0.45.0-linux-amd64.deb
sudo apt install ./ipfs-desktop-0.45.0-linux-amd64.deb
```

After installation, the Desktop app will run in the background and manage your local node.

---

#### 2. Expose the bundled `ipfs` CLI

Unlike Windows/macOS, the Linux Desktop app doesn’t automatically add its bundled `ipfs` binary to your system path.  
To make the same binary available in your terminal:

1.) Locate the bundled binary:
```bash
find /opt /usr -name ipfs 2>/dev/null | grep IPFS
```

Example output:
```
/opt/IPFS Desktop/resources/app.asar.unpacked/node_modules/kubo/kubo/ipfs
```

2.) Create a symlink so your shell can find it:
```bash
IPFS_DESKTOP_IPFS="/opt/IPFS Desktop/resources/app.asar.unpacked/node_modules/kubo/kubo/ipfs"
sudo ln -sf "$IPFS_DESKTOP_IPFS" /usr/local/bin/ipfs
````

> Note: You don’t need to add the directory to `PATH` this way, the symlink makes `ipfs` globally available.

---

#### 3. Verify

Check that your terminal is using the same binary as Desktop:

```bash
which ipfs
ipfs --version
```

You should see `/usr/local/bin/ipfs` pointing to the Desktop app’s binary and the correct version number displayed.

---

> You can run the Desktop app with its embedded node OR a system node. For dev, either is fine, just ensure the API/Gateway settings below match what your apps expect.

---

## 2) Configure IPFS Desktop

Open **IPFS Desktop → Settings → (Config)** and locate the `Addresses` section. Change the Gateway port to **9010**:

```json
"Addresses": {
  "API": "/ip4/127.0.0.1/tcp/5001",
  "Gateway": "/ip4/127.0.0.1/tcp/9010",
  "Swarm": [
    "/ip4/0.0.0.0/tcp/4001",
    "/ip6/::/tcp/4001",
    "/ip4/0.0.0.0/udp/4001/quic-v1",
    "/ip6/::/udp/4001/quic-v1"
  ]
}
```

Now set permissive **CORS** headers for local development under the `API` section. In the same config JSON, ensure this block exists/merges cleanly (don’t duplicate keys):

```json
"API": {
  "HTTPHeaders": {
    "Access-Control-Allow-Origin": [
      "https://webui.ipfs.io",
      "http://webui.ipfs.io.ipns.localhost:9010",
      "http://localhost:*",
      "http://127.0.0.1:*"
    ],
    "Access-Control-Allow-Methods": [
      "GET",
      "POST",
      "PUT",
      "OPTIONS"
    ],
    "Access-Control-Allow-Headers": [
      "*"
    ]
  }
}
```

> **Dev-only escape hatch:** If you hit stubborn CORS errors, you can temporarily set `"Access-Control-Allow-Origin": ["*"]`. It’s convenient for local dev, but **don’t** leave it that way outside of development.

After saving the config, **restart your IPFS node** (quit & reopen IPFS Desktop, or restart the background daemon).

---

## 3) Install IPFS Companion (Browser Extension)

Install the Companion in your **testing browser**:

- docs: [https://docs.ipfs.tech/install/ipfs-companion/](https://docs.ipfs.tech/install/ipfs-companion/)
    

Open the extension → **gear icon** (settings) → find **Local Gateway** and change it from:

- `http://localhost:8080` **to** `http://localhost:9010`
    

Why? You just told IPFS Desktop to serve its **Gateway** on **9010**, so Companion should use the same port.

> **Pro tip:** Keep a second browser (or a separate profile) **without** IPFS Companion enabled to simulate regular users and crawlers.

---

## 4) Verify Your Local IPFS Setup

- Visit `http://localhost:9010/ipfs/<some-known-CID>` — you should see content.
    
- Run `ipfs id` in a terminal (Linux/macOS; on Windows if CLI is installed). You should get your peer info.
    
- If the gateway doesn’t respond, re-check that the Desktop config saved and the node restarted.
    

---

## 5) Clone TruthGate & Project Layout

Grab the repo:

```bash
git clone https://github.com/TruthOrigin/TruthGate-IPFS.git
cd TruthGate-IPFS
```

Inside the solution, the web project is **`TruthGate-Web`**.

---

## 6) Development Host Emulation (run GUI vs. emulate a site)

Open `TruthGate-Web/appsettings.Development.json` and find:

```json
"DevEmulateHost": ""
```

- **Empty string** (`""`): TruthGate runs as if you visited it via a normal host/IP and shows the GUI.
    
- **Set to a domain** (e.g., `"test.com"`): TruthGate will **emulate** loading your environment **as if** you navigated to that domain. This lets you preview a site exactly like production, while staying local.
    

> Make sure any domain you emulate is configured in your local DNS/hosts or test environment as needed, depending on your workflow.

---

## 7) Run the App

Use your usual .NET workflow (IDE run/debug, or CLI). Examples:

```bash
# from the solution or project directory
# restore/build
 dotnet restore
 dotnet build

# run the web project directly (adjust path if needed)
 dotnet run --project TruthGate-Web/TruthGate-Web.csproj
```

- Confirm your app launches and can talk to IPFS at `http://localhost:9010`.
    
- With **DevEmulateHost** set to a domain, browse to your app and confirm it renders as that site would in production.
    

---

## 8) Test Matrix (recommended)

1. **Browser A (with IPFS Companion):**
    
    - Confirm IPFS resources resolve through `localhost:9010`.
        
    - Load emulated site, ensure assets/data served via IPFS behave correctly.
        
2. **Browser B (no IPFS):**
    
    - Confirm your hosted paths and fallbacks work for non-IPFS users.
        
    - Validate SEO/metadata rendering paths you care about.
        

---

## 9) Troubleshooting

- **CORS errors:** Temporarily set `"Access-Control-Allow-Origin": ["*"]` (dev only), restart node, then tighten later.
    
- **Gateway still on 8080:** Re-check the Desktop config for `Addresses.Gateway` and ensure it’s exactly `"/ip4/127.0.0.1/tcp/9010"`. Restart the node.
    
- **CLI not found (Linux):** Install the `ipfs` (Kubo) CLI so `ipfs --version` works. Ensure it’s using the same repo/path as the Desktop node (or that the API/Gateway ports match).
    
- **Companion not using 9010:** Open the extension settings and change **Local Gateway** to `http://localhost:9010`.
    
- **Content not loading by CID:** Try a well-known public CID to rule out local content pinning, or add/pin a small file locally via Desktop/CLI and request it from the gateway.
    

---

## 10) Safety & Dev Hygiene

- Keep permissive CORS **only** for local dev.
    
- Commit only intended changes—avoid committing machine-specific config unless your team agrees.
    
- Use separate browser profiles so you can quickly test with/without IPFS.
    

---

## 11) Next Steps

- Create a small test site and set `DevEmulateHost` to its primary domain (e.g., `"test.com"`).
    
- Add/pin content to IPFS locally and confirm CID-based access at `http://localhost:9010`.
    
- Iterate on your site + TruthGate integration, testing across the with-IPFS/without-IPFS matrix.
    

Happy building! If something feels off, it’s usually ports, CORS, or Companion pointing at the wrong gateway—triple-check those and you’ll be golden.