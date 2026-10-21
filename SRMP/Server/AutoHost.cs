using Lidgren.Network;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SRMultiplayer.Server
{
    /// <summary>
    /// Drives an unattended host: loads a save, opens the lobby and publishes the
    /// friend code, all without anyone touching the multiplayer window. Intended
    /// for running the game headless (Xvfb/Proton) as a de facto server.
    /// </summary>
    public class AutoHost : SRSingleton<AutoHost>
    {
        private const int MainMenuScene = 2;
        private const int GameScene = 3;

        public AutoHostConfig Config { get; private set; }
        public bool IsHosting { get; private set; }
        public bool IsIdlePaused { get; private set; }

        /// <summary>
        /// True while the host character should be unkillable. Gates the god mode
        /// patches so a normal client host is never affected.
        /// </summary>
        public bool IsGodMode { get; private set; }

        private float m_LastStatus;
        private float m_LastSave;
        private float m_LastShutdownCheck;
        private bool m_ShuttingDown;

        private string ShutdownCompletePath
        {
            get { return Path.Combine(SRMP.ModDataPath, "server.stopped"); }
        }

        public override void Awake()
        {
            base.Awake();

            Config = AutoHostConfig.Load();
            Config.ApplyCommandLine(Environment.GetCommandLineArgs());

            if (!Config.Enabled)
            {
                return;
            }

            //claim the username before EpicApplication logs in with it
            Globals.Username = Config.Username;

            //EOS fixes lobby size at creation, so this must be set before hosting
            Globals.MaxPlayers = Mathf.Clamp(Config.MaxPlayers, 2, 64);

            ServerCommands.Operators.Clear();
            if (Config.OperatorProductUserIds != null)
            {
                foreach (var productUserId in Config.OperatorProductUserIds)
                {
                    if (!string.IsNullOrWhiteSpace(productUserId))
                        ServerCommands.Operators.Add(productUserId.Trim());
                }
            }

            ServerLog("=====================================");
            ServerLog(" SRMP headless host starting");
            ServerLog(" username : " + Config.Username);
            ServerLog(" save     : " + (string.IsNullOrEmpty(Config.GameName)
                ? (Config.LoadLatestSave ? "<most recent>" : "<new game>")
                : Config.GameName));
            ServerLog(" slots    : " + Globals.MaxPlayers);
            ServerLog(" operator EOS IDs: " + (ServerCommands.Operators.Count == 0
                ? "<none configured>"
                : string.Join(", ", ServerCommands.Operators.ToArray())));
            ServerLog(" config   : " + AutoHostConfig.ConfigPath);
            ServerLog("=====================================");
        }

        /// <summary>
        /// Frees the game loop to run at the configured rate. A headless server
        /// has no display to synchronise to, and every frame it waits is added to
        /// every connected player's round trip, because incoming packets are only
        /// drained once per frame.
        /// </summary>
        private void ApplyFrameRate(bool hasRemotePlayers)
        {
            QualitySettings.vSyncCount = 0;
            int target = hasRemotePlayers ? Config.TargetFrameRate : Config.IdleTargetFrameRate;
            Application.targetFrameRate = target > 0 ? target : -1;
        }

        private void Start()
        {
            if (!Config.Enabled) return;

            //MultiplayerUI.Start restores the interactive username from PlayerPrefs;
            //component order puts us after it, so re-assert ours here as well
            Globals.Username = Config.Username;

            StartCoroutine(RunHost());
        }

        private IEnumerator RunHost()
        {
            bool menuLoaded = false;
            yield return WaitForMainMenu(ok => menuLoaded = ok);
            if (!menuLoaded)
            {
                AbortStartup("main menu did not become ready before the load timeout");
                yield break;
            }

            yield return new WaitForSecondsRealtime(Config.StartupDelaySeconds);

            bool loggedIn = false;
            yield return WaitForLogin(ok => loggedIn = ok);
            if (!loggedIn)
            {
                AbortStartup("EOS login did not complete before the login timeout");
                yield break;
            }

            bool worldStarted = false;
            Exception worldStartError = null;
            try { worldStarted = LoadWorld(); }
            catch (Exception ex) { worldStartError = ex; }
            if (worldStartError != null)
            {
                AbortStartup("could not start loading the world\n" + worldStartError);
                yield break;
            }
            if (!worldStarted)
            {
                AbortStartup("world loading was not started");
                yield break;
            }

            bool loaded = false;
            yield return WaitForWorld(ok => loaded = ok);
            if (!loaded)
            {
                AbortStartup("world did not finish loading before the load timeout");
                yield break;
            }

            //let the scene settle before the server walks every actor in it
            yield return new WaitForSecondsRealtime(2f);

            ServerLog("[AutoHost] Opening lobby...");
            try
            {
                if (EpicApplication.Instance == null || EpicApplication.Instance.Lobby == null)
                    throw new InvalidOperationException("EOS lobby interface was not initialized");
                EpicApplication.Instance.Lobby.CreateLobby();
            }
            catch (Exception ex)
            {
                AbortStartup("could not create the EOS lobby\n" + ex);
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 60f;
            while (!Globals.IsServer && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!Globals.IsServer)
            {
                AbortStartup("EOS lobby creation timed out");
                yield break;
            }

            IsHosting = true;

            bool hasRemotePlayers = Globals.Players.Values.Any(player => player != null
                && player.ID != Globals.LocalID && player.State == NetworkPlayerState.Connected);
            ApplyFrameRate(hasRemotePlayers);
            ServerLog("[AutoHost] Target frame rate: "
                      + (hasRemotePlayers ? Config.TargetFrameRate : Config.IdleTargetFrameRate)
                      + " fps (" + (hasRemotePlayers ? "active" : "idle") + "), vsync off");

            //the host stays where the save put it. Moving it underground looked
            //tidy but sat inside a kill volume, and the resulting death/respawn
            //loop clobbered the server every frame.
            if (Config.GodMode)
            {
                IsGodMode = true;
                ServerLog("[AutoHost] Host character is in god mode");
            }

            PublishServerCode();
        }

        private void AbortStartup(string reason)
        {
            ServerLog("[AutoHost] Startup failed: " + reason + "; exiting so the launcher can retry");
            Application.Quit();
        }

        /// <summary>Waits for the main menu and game context, bounded by the load timeout.</summary>
        private IEnumerator WaitForMainMenu(Action<bool> result)
        {
            float deadline = Time.realtimeSinceStartup + Config.LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetActiveScene().buildIndex == MainMenuScene
                    && SRSingleton<GameContext>.Instance != null)
                {
                    result(true);
                    yield break;
                }
                yield return null;
            }
            result(false);
        }

        /// <summary>Waits for the anonymous EOS device login to land.</summary>
        private IEnumerator WaitForLogin(Action<bool> result)
        {
            float deadline = Time.realtimeSinceStartup + Config.LoginTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var app = EpicApplication.Instance;
                if (app != null && app.Authentication != null && app.Authentication.IsLoggedIn)
                {
                    ServerLog("[AutoHost] EOS login complete as " + app.Authentication.Username);
                    result(true);
                    yield break;
                }
                yield return null;
            }
            result(false);
        }

        /// <summary>
        /// Starts loading the configured save, or a brand new game when no save is
        /// configured or the configured one is missing.
        /// </summary>
        private bool LoadWorld()
        {
            var autoSave = SRSingleton<GameContext>.Instance.AutoSaveDirector;

            if (!string.IsNullOrEmpty(Config.GameName))
            {
                var summary = FindSave(autoSave, Config.GameName);
                if (summary != null)
                {
                    ServerLog($"[AutoHost] Loading save '{summary.displayName}' (day {summary.day})");
                    autoSave.BeginLoad(summary.name, summary.saveName,
                        () => ServerLog("[AutoHost] Save failed to load"));
                    return true;
                }

                if (!Config.CreateGameIfMissing)
                {
                    ServerLog($"[AutoHost] Save '{Config.GameName}' not found and CreateGameIfMissing is false, aborting");
                    return false;
                }

                ServerLog($"[AutoHost] Save '{Config.GameName}' not found, creating a new game instead");
            }

            //no specific world asked for: continue where the server left off,
            //otherwise every restart silently abandons the previous world
            if (Config.LoadLatestSave)
            {
                var latest = FindLatestSave(autoSave);
                if (latest != null)
                {
                    ServerLog($"[AutoHost] Continuing most recent save '{latest.displayName}' "
                              + $"(day {latest.day}, saved {latest.saveTimestamp:yyyy-MM-dd HH:mm})");
                    autoSave.BeginLoad(latest.name, latest.saveName,
                        () => ServerLog("[AutoHost] Save failed to load"));
                    return true;
                }
                ServerLog("[AutoHost] No existing save found, creating the first world");
            }

            var mode = Config.ResolveGameMode();
            ServerLog($"[AutoHost] Creating new game '{Config.NewGameDisplayName}' ({mode})");
            autoSave.LoadNewGame(Config.NewGameDisplayName, Identifiable.Id.PINK_SLIME, mode,
                () => ServerLog("[AutoHost] New game failed to load"));
            return true;
        }

        /// <summary>
        /// Resolves a configured save name against the internal game name first and
        /// the display name second, picking the newest save of that game.
        /// </summary>
        private GameData.Summary FindSave(AutoSaveDirector autoSave, string wanted)
        {
            try
            {
                var games = autoSave.AvailableGamesByGameName();
                if (games == null) return null;

                foreach (var entry in games)
                {
                    var saves = entry.Value?.Where(s => s != null && !s.isInvalid).ToList();
                    if (saves == null || saves.Count == 0) continue;

                    bool match = string.Equals(entry.Key, wanted, StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        match = saves.Any(s => string.Equals(s.displayName, wanted, StringComparison.OrdinalIgnoreCase));
                    }

                    if (match)
                    {
                        return saves.OrderByDescending(s => s.saveTimestamp).First();
                    }
                }
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not enumerate saves\n" + ex);
            }
            return null;
        }

        /// <summary>
        /// The newest valid save across every world. The game's own
        /// GetSaveToContinue is preferred so the server picks the same world the
        /// Continue button would; scanning is only a fallback for when that is
        /// unavailable.
        /// </summary>
        private GameData.Summary FindLatestSave(AutoSaveDirector autoSave)
        {
            try
            {
                var continueSave = autoSave.GetSaveToContinue();
                if (continueSave != null && !continueSave.isInvalid)
                {
                    return continueSave;
                }
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] GetSaveToContinue failed, scanning instead\n" + ex);
            }

            try
            {
                var games = autoSave.AvailableGamesByGameName();
                if (games == null) return null;

                GameData.Summary newest = null;
                foreach (var entry in games)
                {
                    if (entry.Value == null) continue;
                    foreach (var save in entry.Value)
                    {
                        if (save == null || save.isInvalid) continue;
                        if (newest == null || save.saveTimestamp > newest.saveTimestamp)
                        {
                            newest = save;
                        }
                    }
                }
                return newest;
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not scan saves\n" + ex);
                return null;
            }
        }

        /// <summary>Waits for the game scene to report itself fully loaded.</summary>
        private IEnumerator WaitForWorld(Action<bool> result)
        {
            float deadline = Time.realtimeSinceStartup + Config.LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (Globals.GameLoaded && SceneManager.GetActiveScene().buildIndex == GameScene)
                {
                    result(true);
                    yield break;
                }
                yield return null;
            }
            result(false);
        }

        /// <summary>Writes the friend code to stdout, the log and a file operators can read.</summary>
        private void PublishServerCode()
        {
            string code = Globals.ServerCode;

            ServerLog("=====================================");
            ServerLog(" SRMP server is up");
            ServerLog(" FRIEND CODE : " + code);
            ServerLog(" world       : " + Globals.CurrentGameName);
            ServerLog("=====================================");

            if (string.IsNullOrEmpty(Config.ServerCodeFile)) return;

            try
            {
                string path = Path.Combine(SRMP.ModDataPath, Config.ServerCodeFile);
                File.WriteAllText(path, code);
                ServerLog("[AutoHost] Friend code written to " + path);
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not write friend code file\n" + ex);
            }
        }

        /// <summary>Absolute path of the file that requests a clean shutdown.</summary>
        public string ShutdownRequestPath
        {
            get { return Path.Combine(SRMP.ModDataPath, Config.ShutdownRequestFile); }
        }

        /// <summary>
        /// Saves the world, tells everyone why, then quits. A container stop
        /// cannot do this from outside: Unity will not flush a save in response
        /// to a signal, so the request has to be handled in-process.
        /// </summary>
        private IEnumerator ShutdownSequence()
        {
            ServerLog("[AutoHost] Shutdown requested, saving world...");

            try
            {
                new PacketPlayerChat { message = "Server is shutting down, saving..." }
                    .SendToAll(NetDeliveryMethod.ReliableOrdered);
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not announce shutdown\n" + ex);
            }

            bool saved = false;
            try
            {
                SRSingleton<GameContext>.Instance.AutoSaveDirector.SaveAllNow();
                saved = true;
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Save failed during shutdown\n" + ex);
            }

            //Use realtime: an empty server has paused game time and must still
            //be able to flush a save and stop cleanly.
            yield return new WaitForSecondsRealtime(saved ? 3f : 1f);

            if (saved) ServerLog("[AutoHost] World saved");

            try
            {
                if (File.Exists(ShutdownRequestPath)) File.Delete(ShutdownRequestPath);
            }
            catch { /* the container is going away anyway */ }

            //close the lobby before quitting: members get a membership-closed
            //event and return to the menu, instead of waiting out a timeout in a
            //world that is no longer being hosted
            try
            {
                EpicApplication.Instance.Lobby.DestroyLobby();
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not close the lobby cleanly\n" + ex);
            }

            //give the close a moment to reach everyone before the process dies
            yield return new WaitForSecondsRealtime(2f);

            try
            {
                File.WriteAllText(ShutdownCompletePath, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                ServerLog("[AutoHost] Could not write clean-shutdown marker\n" + ex);
            }

            ServerLog("[AutoHost] Closing lobby and quitting");

            //SRMP.OnDestroy and EpicApplication.OnApplicationQuit tear the lobby
            //and the EOS platform down as the application exits
            Application.Quit();
        }

        private void Update()
        {
            if (!IsHosting) return;

            if (!m_ShuttingDown && !Globals.IsServer)
            {
                ServerLog("[AutoHost] EOS/P2P host stopped unexpectedly; exiting so the launcher can retry");
                Application.Quit();
                return;
            }

            bool hasRemotePlayers = Globals.Players.Values.Any(player =>
                player != null && player.ID != Globals.LocalID
                && player.State == NetworkPlayerState.Connected);
            bool shouldPauseForEmptyServer = Config.PauseWhenNoPlayers && !hasRemotePlayers;
            if (shouldPauseForEmptyServer != IsIdlePaused)
            {
                IsIdlePaused = shouldPauseForEmptyServer;
                Time.timeScale = IsIdlePaused ? 0f : 1f;
                ApplyFrameRate(hasRemotePlayers);
                ServerLog("[AutoHost] Loop rate changed to "
                          + (hasRemotePlayers ? Config.TargetFrameRate : Config.IdleTargetFrameRate)
                          + " fps");
                ServerLog(IsIdlePaused
                    ? "[AutoHost] No remote players remain; pausing world time"
                    : "[AutoHost] A remote player connected; resuming world time");

                if (IsIdlePaused)
                {
                    try
                    {
                        SRSingleton<GameContext>.Instance.AutoSaveDirector.SaveAllNow();
                        m_LastSave = Time.realtimeSinceStartup;
                        ServerLog("[AutoHost] Saved world before idle pause");
                    }
                    catch (Exception ex)
                    {
                        ServerLog("[AutoHost] Save before idle pause failed\n" + ex);
                    }
                }
            }

            //poll rather than use a FileSystemWatcher: this has to work across a
            //bind mount, where watcher events are not reliably delivered
            if (!m_ShuttingDown
                && Time.realtimeSinceStartup - m_LastShutdownCheck > 1f)
            {
                m_LastShutdownCheck = Time.realtimeSinceStartup;
                try
                {
                    if (!string.IsNullOrEmpty(Config.ShutdownRequestFile)
                        && File.Exists(ShutdownRequestPath))
                    {
                        m_ShuttingDown = true;
                        StartCoroutine(ShutdownSequence());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ServerLog("[AutoHost] Could not check for shutdown request\n" + ex);
                }
            }

            if (m_ShuttingDown) return;

            if (Config.StatusIntervalSeconds > 0f
                && Time.realtimeSinceStartup - m_LastStatus > Config.StatusIntervalSeconds)
            {
                m_LastStatus = Time.realtimeSinceStartup;
                var names = Globals.Players.Values
                    .Where(p => p != null && p.ID != Globals.LocalID
                        && p.State == NetworkPlayerState.Connected)
                    .Where(p => !string.IsNullOrEmpty(p.Username))
                    .Select(p => p.Username)
                    .ToList();
                //the game reapplies its own quality settings at various points,
                //so this is re-asserted rather than set once
                ApplyFrameRate(names.Count > 0);

                ServerLog($"[AutoHost] code {Globals.ServerCode} | {SRMP.MeasuredFps} fps | "
                          + $"{names.Count} player(s) online"
                          + (names.Count > 0 ? ": " + string.Join(", ", names.ToArray()) : ""));

                if (SRMP.MeasuredFps > 0 && SRMP.MeasuredFps < 20)
                {
                    ServerLog("[AutoHost] WARNING: the server loop is slow. Every player's ping and "
                              + "world sync is limited by this, because packets are only handled once "
                              + "per frame. Give the container more CPU.");
                }
            }

            if (!IsIdlePaused
                && Config.AutoSaveIntervalSeconds > 0f
                && Time.realtimeSinceStartup - m_LastSave > Config.AutoSaveIntervalSeconds)
            {
                m_LastSave = Time.realtimeSinceStartup;
                try
                {
                    SRSingleton<GameContext>.Instance.AutoSaveDirector.SaveAllNow();
                    ServerLog("[AutoHost] World saved");
                }
                catch (Exception ex)
                {
                    ServerLog("[AutoHost] Save failed\n" + ex);
                }
            }
        }

        /// <summary>
        /// Logs to the SRMP log and to stdout, so a headless wrapper can follow the
        /// server without parsing the Unity player log.
        /// </summary>
        public static void ServerLog(string message)
        {
            SRMP.Log(message);
            try
            {
                Console.WriteLine("[SRMP] " + message);
            }
            catch { /* no console attached; the SRMP log still has it */ }
        }
    }
}
