#if SRML
using Newtonsoft.Json;
using SRML;
using SRML.SR;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SRML.SR.UI.Utils;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Patches.ModCompat;
using UnityEngine;

namespace SRMultiplayer
{
    /// <summary>
    /// Handles mod being loaded from SRML
    /// Takes the place of the Main Standalone file, but caters to the SRML
    /// </summary>
    public class MainSRML : ModEntryPoint
    {
        private static GameObject m_GameObject;

        // Called before GameContext.Awake
        // this is where you want to register stuff (like custom enum values or identifiable id's)
        // and patch anything you want to patch with harmony
        public override void PreLoad()
        {
            base.PreLoad();
        }


        // Called right before PostLoad
        // Used to register stuff that needs lookupdirector access
        public override void Load()
        {
            if (m_GameObject != null) return;

            SRMP.Log("Loading SRMP SRML Version");

            Utils.LoadUserData();

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

            //mark the mod as a background task
            Application.runInBackground = true;

            //initialize connect to the harmony patcher
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            
            // Initialize ModCompat Patches
            if (SRModLoader.IsModPresent("nicknames")) // Nicknames
                SRMP.CompatPatch(HarmonyInstance, "Nicknames.NicknameCommand", "Execute", typeof(NicknameCommand_Execute));
        }


        // Called after GameContext.Start
        // stuff like gamecontext.lookupdirector are available in this step, generally for when you want to access
        // ingame prefabs and the such
        public override void PostLoad()
        {

        }
    }
}
#endif
