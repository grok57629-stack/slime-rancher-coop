# 🌈 Slime Rancher Co-op (SRMP Revival) — User Guide

> **Play Slime Rancher 1 online with your friends!**  
> Fully synchronized slimes, corrals, plorts, inventory, vacpack, and ranch world.  
> [Читать на русском](README_RU.md) | [Main Page](README.md) | [Credits & Acknowledgements](CREDITS.md)

---

## 📋 Overview

**SRMP Revival** is a hardened, bug-fixed revival of the **Slime Rancher MultiPlayer (SRMP)** mod for **Slime Rancher 1**.

### 🌟 What is Synchronized:
- **Players**: 3D character puppets, smooth interpolation, animations, head rotation, and live markers on the in-game map (`M`).
- **Vacpack & Inventory**: Vacuuming, shooting items, carrying objects, and player newbucks currency.
- **Slimes & World**: Slime physics, emotions, hunger, largo creation, feral slimes, tarr outbreaks, and gordos.
- **Ranch Plots**: Corrals, gardens, silos, auto-feeders, plort collectors, and incinerators.
- **Drones & Gadgets**: Automated drone behavior and slime science gadget placements.

---

## ⚡ Quick Start (One-Click)

### Option 1: Via Graphical Co-op Manager (Recommended)
1. Double-click `launcher/Coop-Manager.cmd`.
2. Confirm your game path (the manager auto-detects Steam installs).
3. Set your desired **Player Nickname**.
4. Click **`⚡ Install Co-op`**.
5. Click **`🚀 Launch Game`**.

### Option 2: Via Command Script
1. Run `launcher/Install-Coop.cmd`.
2. The script creates a backup of `Assembly-CSharp.dll`, patches the `SRML` mod loader, and places `SRMP.dll` into `SRML/Mods`.
3. Launch Slime Rancher through Steam as usual.

---

## 🌐 Networking Modes

SRMP Revival supports two connection methods:

### Mode 1: Zero Port-Forwarding (EOS Server Code) — RECOMMENDED
*Requires NO public IP address, NO router port-forwarding, and NO third-party VPNs (Hamachi/Radmin).*

1. **Host**:
   - Load your world in Slime Rancher.
   - Press **`F4`** to open the multiplayer menu.
   - Click **`Host Game`** under the EOS section.
   - The game generates an alphanumeric **Server Code**.
   - Send this code to your friend.
2. **Client (Friend)**:
   - Launch Slime Rancher.
   - Press **`F4`**.
   - Paste the code into the **`Server Code`** field.
   - Click **`Connect`**. Connection will be established via P2P NAT punchthrough relay!

### Mode 2: Direct IP / LAN / Radmin / Tailscale
*Ideal for local networks or dedicated servers.*

1. **Host**:
   - Press **`F4`** -> **`Host Game`** (default port: `6996` or `16500`).
2. **Client**:
   - Enter host IP and Port, then click **`Connect`**.

---

## 🎮 Controls & Chat Commands

| Key / Command | Action |
|---|---|
| **`F4`** | Toggle multiplayer side-panel & player list |
| **`Enter`** | Open in-game text chat |
| **`M`** | Open game map (friend positions and rotations shown live) |
| **`/home`** | Instant teleport back to the ranch |
| **`/ping`** | Show round-trip latency to host |
| **`/tps`** | Show server simulation tick rate |
| **`/list`** | List all online players |
| **`/sleep <hours>`** | Skip in-game time without waiting |

---

## 🖥️ Dedicated Server (24/7 Hosting)

If you wish to host a persistent ranch world for your friend group:
1. Run `serveur/run.cmd` (or launch via `Coop-Manager`).
2. The server runs as a lightweight headless process.
3. **AutoHost Idle Pause**: When 0 players are connected, the world automatically freezes so slimes don't starve or despawn while everyone is offline.

---

## ↩️ Uninstallation (Revert to Vanilla)

- Run `launcher/Uninstall-Coop.cmd` (or click *Revert to Vanilla* in `Coop-Manager`).
- Restores the original `Assembly-CSharp.dll` and deletes mod files. Slime Rancher becomes 100% vanilla.

---

## 👥 Credits & Acknowledgements

- **Saty / SatyPardus** — Original creator and architect of SRMP.
- **Shunikai972** — Author of SRMP Revival fork (160 stability fixes and test harness).
- **SlimeRancherModding** — Slime Rancher Mod Loader (SRML).
- **cranebeaker / coop-anything** — Automated QA guidelines and playbook.
- **grok57629-stack** — Launcher, packaging, and bilingual documentation.
