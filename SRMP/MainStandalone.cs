#if Standalone
using HarmonyLib;
using Newtonsoft.Json;
using SRMultiplayer.Networking;
using System.IO;
using System.Reflection;
using SRMultiplayer.EpicSDK;
using UnityCoreMod;
using UnityEngine;

namespace SRMultiplayer
{   // <summary>
    /// Handles mod being loaded from directly without the mod loader
    /// </summary>
    public class MainStandalone : IUnityMod
    {
        private static GameObject m_GameObject;

        public void Load()
        {
            if (m_GameObject != null) return;

            SRMP.Log("Loading SRMP Standalone Version");

            Utils.LoadUserData();

            //create the mods main game objects and start connecting everything
            string[] args = System.Environment.GetCommandLineArgs();

            m_GameObject = new GameObject("SRMP");
            m_GameObject.AddComponent<SRMP>();
            m_GameObject.AddComponent<MultiplayerUI>();
            m_GameObject.AddComponent<ChatUI>();
            m_GameObject.AddComponent<SRMPConsole>();
            //added after MultiplayerUI so it can override the interactive username,
            //and before EpicApplication which logs in using that username
            m_GameObject.AddComponent<SRMultiplayer.Server.AutoHost>();
            m_GameObject.AddComponent<EpicApplication>();

            //mark all mod objects and do not destroy
            GameObject.DontDestroyOnLoad(m_GameObject);

            //get current mod version
            Globals.Version = Assembly.GetExecutingAssembly().GetName().Version.Revision;

            //initialize harmony and init the patches
            var harmony = new Harmony("saty.mod.srmp");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            //mark the mod as a background task
            Application.runInBackground = true;
        }

        public void Unload() { }
    }
}
#endif
