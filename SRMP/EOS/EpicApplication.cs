using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.IntegratedPlatform;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SRMultiplayer;
using SRMultiplayer.Networking;
using UnityEngine;

namespace SRMultiplayer.EpicSDK
{
    public class EpicApplication : SRSingleton<EpicApplication>
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("Kernel32.dll")]
        private static extern int FreeLibrary(IntPtr hLibModule);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private PlatformInterface epicPlatformInterface;
        private bool platformInitialized;
        
        public EpicAuthentication Authentication { get; private set; }
        public EpicMetrics Metrics { get; private set; }
        public EpicLobby Lobby { get; private set; }

        private void Start()
        {
            epicPlatformInterface = Initialize(PlatformFlags.DisableOverlay);

            if(epicPlatformInterface != null)
            {
                Authentication = new EpicAuthentication(epicPlatformInterface.GetConnectInterface());
                Metrics = new EpicMetrics(epicPlatformInterface.GetMetricsInterface());
                Lobby = new EpicLobby(epicPlatformInterface.GetLobbyInterface());
                
                
            }
        }

        private void Update()
        {
            epicPlatformInterface?.Tick();
            Lobby?.Tick();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        internal void Shutdown()
        {
            var lobby = Lobby;
            if (lobby != null && lobby.IsInLobby)
            {
                try
                {
                    if (lobby.IsLobbyOwner)
                    {
                        lobby.DestroyLobby();
                    }
                    else
                    {
                        lobby.LeaveLobby();
                    }
                }
                catch (Exception ex)
                {
                    SRMP.Log("[EOS] Could not close lobby during application shutdown: " + ex);
                }
            }

            try
            {
                epicPlatformInterface?.Release();
            }
            catch (Exception ex)
            {
                SRMP.Log("[EOS] Could not release platform interface: " + ex);
            }
            epicPlatformInterface = null;

            if (platformInitialized)
            {
                platformInitialized = false;
                try
                {
                    PlatformInterface.Shutdown();
                }
                catch (Exception ex)
                {
                    SRMP.Log("[EOS] Could not shut down platform: " + ex);
                }
            }
        }
        public static byte[] ExtractResource(String filename)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream resFilestream = a.GetManifestResourceStream(filename))
            {
                if (resFilestream == null) return null;
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
        private PlatformInterface Initialize(PlatformFlags platformFlags = PlatformFlags.None)
        {
            if(!Directory.Exists(Path.Combine(Application.dataPath, "SRMP Libs")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "SRMP Libs"));
            }
            var eosSdkPath = Path.Combine(Application.dataPath, "SRMP Libs", "EOSSDK-Win64-Shipping.dll");
            if (!File.Exists(eosSdkPath))
            {
                File.WriteAllBytes(eosSdkPath, ExtractResource("SRMultiplayer.EOS.Libs.EOSSDK-Win64-Shipping.dll"));
            }

            var libraryPointer = LoadLibrary(eosSdkPath);
            if (libraryPointer == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library from {eosSdkPath}");
            }

            Bindings.Hook(libraryPointer, GetProcAddress);
            WindowsBindings.Hook(libraryPointer, GetProcAddress);

            var initializeOptions = new InitializeOptions()
            {
                ProductName = "Slime Rancher Multiplayer",
                ProductVersion = $"1.0.{Globals.Version}"
            };

            Result initializeResult = PlatformInterface.Initialize(ref initializeOptions);
            SRMultiplayer.SRMP.Log(initializeResult.ToString());
            if (initializeResult != Result.Success && initializeResult != Result.AlreadyConfigured)
            {
                SRMultiplayer.SRMP.Log("[EOS] Platform initialization failed; skipping platform creation");
                return null;
            }
            //If another EOS integration initialized the global SDK first, we
            //can still create this platform but must not shut its SDK down.
            platformInitialized = initializeResult == Result.Success;

            LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Warning);
            LoggingInterface.SetCallback((ref LogMessage message) => SRMultiplayer.SRMP.Log(message.Message));
            
            // do not use this please!
            var options = new WindowsOptions()
            {
                ProductId = "4f587a9e97c94f05bd466f0a24293c70",
                SandboxId = "405807cb1bed429d958a81486e6fe44e",
                ClientCredentials = new ClientCredentials()
                {
                    ClientId = "xyza7891xQUFgUz4PPPn7aserqsE1FAy",
                    ClientSecret = "Dfr7ZOvt9b+EeL4jw9gq1gsngeWf2Hs2prVqCEw6gj8"
                },
                DeploymentId = "cf520e9a63fa463bb44b862586c129b2",
                Flags = platformFlags,
                IsServer = false
            };

            PlatformInterface platformInterface = PlatformInterface.Create(ref options);

            if (platformInterface == null)
            {
                SRMultiplayer.SRMP.Log($"Failed to create platform. Ensure the relevant Settings are set or passed into the application as arguments.");
            }

            
            return platformInterface;
            
        }

        public NetworkClient CreateClient()
        {
            return new NetworkClient(epicPlatformInterface.GetP2PInterface());
        }

        public NetworkServer CreateServer()
        {
            return new NetworkServer(epicPlatformInterface.GetP2PInterface());
        }
    }
}
