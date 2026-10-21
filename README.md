<h1 align="center">Slime Rancher Co-op (SRMP Revival)</h1>

<p align="center">
  <b>Play Slime Rancher 1 online with your friends! / Играйте в Slime Rancher 1 по сети с друзьями!</b>
</p>

<p align="center">
  <a href="#-русский"><img src="https://img.shields.io/badge/Язык-Русский-blue?style=for-the-badge" alt="Русский"></a>
  <a href="#-english"><img src="https://img.shields.io/badge/Language-English-red?style=for-the-badge" alt="English"></a>
  <a href="CREDITS.md"><img src="https://img.shields.io/badge/Credits-Authors-brightgreen?style=for-the-badge" alt="Credits"></a>
  <img src="https://img.shields.io/badge/QA_Level-Automated_160%2F160_PASS-success?style=for-the-badge" alt="QA">
</p>

---

## 📑 Language Navigation / Выбор языка

- 🇷🇺 **[Перейти к русской версии](#-русский)** | [Полная инструкция (RU)](README_RU.md)
- 🇬🇧 **[Go to English version](#-english)** | [Full Guide (EN)](README_EN.md)
- 👥 **[Credits & Acknowledgements / Авторы проекта](CREDITS.md)**

---

<h2 id="-русский">🇷🇺 Русский</h2>

### О проекте
**SRMP Revival** — стабильная и доработанная версия модификации **Slime Rancher MultiPlayer (SRMP)** для игры **Slime Rancher 1**. Включает полную синхронизацию игрового мира, слаймов, загонов, сборщиков плортов, вак-пака и инвентаря.

### 🌟 Преимущества:
- **Подключение без проброса портов (EOS Server Code)**: соединение по короткому коду через P2P NAT-релеи — никаких белых IP или сторонних VPN.
- **Поддержка LAN / Direct IP**: прямое подключение по локальной сети или выделенному серверу.
- **Графический менеджер `Coop-Manager.cmd`**: установка, настройка ника, запуск игры и возврат к оригиналу в один клик.
- **Синхронизация карты**: друзья отображаются маркерами с направлением взгляда на карте (`M`).
- **160/160 автотестов**: проверено против стабильных бинарных контрактов Unity Mono.

### 🎮 Быстрый старт:
1. Запустите `launcher/Coop-Manager.cmd` (или `launcher/Install-Coop.cmd`).
2. Нажмите **`⚡ Установить кооп`**.
3. Запустите **Slime Rancher** (через Steam).
4. В игре нажмите клавишу **`F4`** — откроется панель мультиплеера:
   - Хост нажимает **Host Game** и передает другу **Server Code**.
   - Друг вставляет **Server Code** и нажимает **Connect**.
5. Внутриигровой чат: клавиша **`Enter`** (доступны команды `/home`, `/ping`, `/tps`, `/sleep <часы>`).

👉 **[Подробная инструкция на русском языке](README_RU.md)**

---

<h2 id="-english">🇬🇧 English</h2>

### About
**SRMP Revival** is a hardened, fully tested multiplayer modification for **Slime Rancher 1**, based on the pioneering work of Saty's SRMP and Shunikai972's revival.

### 🌟 Key Highlights:
- **Zero Port-Forwarding (EOS Server Code)**: Seamless peer-to-peer connection via Epic Online Services NAT-traversal relay without port forwarding.
- **LAN & Direct IP Support**: Traditional high-performance UDP networking for local setups and dedicated servers.
- **Graphical Manager `Coop-Manager.cmd`**: One-click installer, nickname setter, game launcher, and clean vanilla rollbacks.
- **Live Map Markers**: Player positions, look directions, and zones displayed in real-time on the map (`M`).
- **160/160 Automated QA**: Comprehensive regression and assembly tests passing against the official game build.

### 🎮 Quick Start:
1. Run `launcher/Coop-Manager.cmd` (or `launcher/Install-Coop.cmd`).
2. Click **`⚡ Install Co-op`**.
3. Launch **Slime Rancher** through Steam.
4. In-game, press **`F4`** to toggle the multiplayer overlay:
   - Host clicks **Host Game** and shares the generated **Server Code**.
   - Client pastes the **Server Code** and clicks **Connect**.
5. Press **`Enter`** to chat (useful commands: `/home`, `/ping`, `/tps`, `/sleep <hours>`).

👉 **[Comprehensive English User Guide](README_EN.md)**

---

## 👥 Credits & Authors / Авторы

- **[Saty / SatyPardus](https://github.com/SatyPardus)** — Original author & architect of SRMP.
- **[Shunikai972](https://github.com/Shunikai972)** — SRMP Revival fork (stability fixes & 160-test suite).
- **[SlimeRancherModding](https://github.com/SlimeRancherModding/SRML)** — Slime Rancher Mod Loader (SRML).
- **[cranebeaker / coop-anything](https://github.com/cranebeaker/coop-anything)** — Research methodology, test harness standards, and playbooks.
- **[grok57629-stack](https://github.com/grok57629-stack)** — Visual launcher, 1-click installer, bilingual documentation, and packaging.

See [CREDITS.md](CREDITS.md) for full details.
