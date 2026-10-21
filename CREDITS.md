# Credits & Acknowledgements / Авторы и благодарности

This project builds upon the immense work of the Slime Rancher modding community. We express our deepest gratitude to all contributors and authors:

### 🌟 Original Creator
- **[Saty / SatyPardus](https://github.com/SatyPardus)** — The original creator and architect of **SRMP** (Slime Rancher MultiPlayer). Without Saty's pioneering reverse engineering of Slime Rancher 1, network packet architecture, and custom puppet avatars, multiplayer in Slime Rancher would not exist.
  - Original Repository: [SatyPardus/Slime-Rancher-Multiplayer](https://github.com/SatyPardus)
  - Itch.io: [Slime Rancher Multiplayer Mod](https://satypardus.itch.io/slime-rancher-multiplayer-mod)

### 🛠️ Revival & Stability Fork
- **[Shunikai972](https://github.com/Shunikai972)** — Author of the SRMP Revival fork (2026).
  - Cleaned up build warnings and unmanaged memory allocations.
  - Fixed packet scope leaks, plort collector authority fallbacks, and stale consumable null-reference crashes.
  - Implemented the 160-check automated regression and assembly test suite (`TESTING.md` / `AUDIT_STABILITY.md`).
  - Repository: [Slime-rancher-multiplayer-public-mode](https://github.com/Shunikai972/Slime-rancher-multiplayer-public-mode)

### 🔧 Modding Framework
- **[SlimeRancherModding (SRML)](https://github.com/SlimeRancherModding/SRML)** — The official Slime Rancher Mod Loader team.
  - Provides the underlying Mono.Cecil runtime injector and Harmony patching pipeline.

### 📚 Methodology & QA Standards
- **[cranebeaker / coop-anything](https://github.com/cranebeaker/coop-anything)** — Research methodology, test playbooks, and multi-instance automated verification guidelines.

### 🚀 Package, Launcher & Documentation
- **[grok57629-stack](https://github.com/grok57629-stack)** — Visual WPF Co-op Manager & Launcher, bilingual documentation (RU/EN), 1-click automated installation/rollback scripts, and packaging.
