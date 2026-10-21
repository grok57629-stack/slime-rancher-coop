//using MonomiPark.SlimeRancher.Regions;
//using SRMultiplayer.Networking;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;
//using UnityEngine;
//using UnityEngine.SceneManagement;

/*
 * TestUi is a version of the multiplayer ui that adds the following information
 * Added
    Chat
    Cheats


 * More stats
    Quick silver race is active
    client status 
    server status
    server code
    send and recieve sizes
    packet sizes
 */



//namespace SRMultiplayer
//{
//    class TestUI : SRSingleton<TestUI>
//    {
//        private Rect windowRect = new Rect(Screen.width - 300, 20, 300, 500);
//        private Rect cheatRect = new Rect(20, 20, 300, Screen.height - 40);
//        private string ipaddress = "localhost";
//        private string port = "1337";
//        private string servercode = "";

//        private bool selectSave;
//        private List<string> saveFolders;
//        private bool openMenu = true;
//        private Vector2 chatScroll;
//        private Vector2 saveScroll;
//        private string chatMessage;
//        private List<string> chatMessages = new List<string>();
//        public bool isPublicServer;
//        private bool isSharedCurrency;
//        private bool isSharedUpgrades;
//        private int state;
//        private string saveName;
//        public bool cheat;
//        private string spawnName;
//        private int spawnAmount = 1;
//        private int cheatMenuType = 0;
//        private bool usernameSelected;
//        public List<GingerPatchNode> gingers = new List<GingerPatchNode>();
//        public List<KookadobaPatchNode> kookadobas = new List<KookadobaPatchNode>();

//        public float lastCodeUse;
//        public string LastError;

//        public override void Awake()
//        {
//            base.Awake();

//            Globals.Username = PlayerPrefs.GetString("username", "Username");
//            ipaddress = PlayerPrefs.GetString("ipaddress", "localhost");
//            port = PlayerPrefs.GetString("port", "1337");
//            saveName = PlayerPrefs.GetString("gamename", "srmp");
//        }

//        public void AddChatMessage(string username, string message)
//        {
//            chatMessages.Add(username + ": " + message);
//            chatScroll = new Vector2(0, 100000);
//            if (chatMessages.Count > 100)
//            {
//                chatMessages.RemoveAt(0);
//            }
//        }

//        private void Update()
//        {
//            float prevTime = lastCodeUse;
//            lastCodeUse -= Time.deltaTime;
//            if (prevTime > 0 && lastCodeUse <= 0)
//            {
//                LastError = "Could not connect with Server code. Try restarting the game or use Port forwarding instead";
//            }
//            if (Input.GetKeyDown(KeyCode.F4))
//            {
//                openMenu = !openMenu;
//            }
//        }

//        private void OnGUI()
//        {
//            if (Globals.IsMultiplayer && Input.GetKey(KeyCode.Tab))
//            {
//                GUI.BeginGroup(new Rect(Screen.width / 2 - 100, 50, 200, Globals.Players.Count * 25), GUI.skin.box);
//                foreach (var player in Globals.Players.Values.ToList())
//                {
//                    if (player.ID == Globals.LocalID)
//                    {
//                        GUILayout.Label($"{player.Username} ({player.ID}) {(player == null ? Vector3.zero : SRSingleton<SceneContext>.Instance.Player.transform.position)}");
//                    }
//                    else
//                    {
//                        GUILayout.Label($"{player.Username} ({player.ID}) {(player == null ? Vector3.zero : player.transform.position)}");
//                    }
//                }
//                GUI.EndGroup();
//            }

//            if (!openMenu) return;

//            if (SceneManager.GetActiveScene().buildIndex >= 2)
//            {
//                windowRect = GUI.Window(20, windowRect, MultiplayerWindow, "SRMP v" + Globals.Version);
//                if (cheat)
//                {
//                    cheatRect = GUI.Window(21, cheatRect, MultiplayerWindow, "You damn cheater");
//                }
//            }
//        }

//        private void MultiplayerWindow(int id)
//        {
//            cheatRect.height = Mathf.Min(Screen.height - 40, 800);
//            if (id == 21)
//            {
//                if (GUILayout.Button("All gadgets"))
//                {
//                    foreach (var data in (Gadget.Id[])Enum.GetValues(typeof(Gadget.Id)))
//                    {
//                        SRSingleton<SceneContext>.Instance.GadgetDirector.model.gadgets[data] = 100;
//                    }
//                    foreach (var data in Identifiable.CRAFT_CLASS.Union(Identifiable.PLORT_CLASS))
//                    {
//                        SRSingleton<SceneContext>.Instance.GadgetDirector.model.craftMatCounts[data] = 10000;
//                    }
//                }
//                if (GUILayout.Button("All Upgrades"))
//                {
//                    SRSingleton<SceneContext>.Instance.PlayerState.model.SetUpgrades(((PlayerState.Upgrade[])Enum.GetValues(typeof(PlayerState.Upgrade))).ToList());
//                }
//                if (GUILayout.Button("Get rich"))
//                {
//                    SRSingleton<SceneContext>.Instance.PlayerState.AddCurrency(1000000);
//                }
//                if (GUILayout.Button("Remove all actors"))
//                {
//                    foreach (var actor in SRSingleton<SceneContext>.Instance.GameModel.AllActors().Values.ToList())
//                    {
//                        if (actor != null && actor.transform != null && actor.transform.gameObject.activeInHierarchy && !Identifiable.SCENE_OBJECTS.Contains(actor.ident))
//                        {
//                            Destroyer.DestroyActor(actor.transform.gameObject, "Get.Removed");
//                        }
//                    }
//                }
//                GUILayout.BeginHorizontal();
//                if (GUILayout.Button("Sleep 1"))
//                {
//                    SRSingleton<SceneContext>.Instance.TimeDirector.FastForwardTo(SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(1));
//                }
//                if (GUILayout.Button("Sleep 6"))
//                {
//                    SRSingleton<SceneContext>.Instance.TimeDirector.FastForwardTo(SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(6));
//                }
//                GUILayout.EndHorizontal();
//                GUILayout.BeginHorizontal();
//                if (GUILayout.Button("Sleep 12"))
//                {
//                    SRSingleton<SceneContext>.Instance.TimeDirector.FastForwardTo(SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(12));
//                }
//                if (GUILayout.Button("Sleep 24"))
//                {
//                    SRSingleton<SceneContext>.Instance.TimeDirector.FastForwardTo(SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(24));
//                }
//                GUILayout.EndHorizontal();
//                GUILayout.BeginHorizontal();
//                if (GUILayout.Button("Spawn"))
//                {
//                    cheatMenuType = 0;
//                }
//                if (GUILayout.Button("TP"))
//                {
//                    cheatMenuType = 1;
//                }
//                if (GUILayout.Button("Unlock"))
//                {
//                    cheatMenuType = 2;
//                }
//                if (GUILayout.Button("Exchange"))
//                {
//                    cheatMenuType = 3;
//                }
//                GUILayout.EndHorizontal();
//                GUILayout.BeginHorizontal();
//                if (GUILayout.Button("Gingers"))
//                {
//                    cheatMenuType = 4;
//                    gingers = Resources.FindObjectsOfTypeAll<GingerPatchNode>().ToList();
//                }
//                if (GUILayout.Button("Kookadobas"))
//                {
//                    cheatMenuType = 5;
//                    kookadobas = Resources.FindObjectsOfTypeAll<KookadobaPatchNode>().ToList();
//                }
//                if (GUILayout.Button("Phase Sites"))
//                {
//                    cheatMenuType = 6;
//                }
//                GUILayout.EndHorizontal();
//                if (cheatMenuType == 0)
//                {
//                    spawnName = GUILayout.TextField(spawnName);
//                    int.TryParse(GUILayout.TextField(spawnAmount.ToString()), out spawnAmount);
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var ident in (Identifiable.Id[])Enum.GetValues(typeof(Identifiable.Id)))
//                    {
//                        try
//                        {
//                            if (ident != Identifiable.Id.NONE && ident != Identifiable.Id.PLAYER)
//                            {
//                                if (string.IsNullOrWhiteSpace(spawnName) || ident.ToString().ToLower().Contains(spawnName.ToLower()))
//                                {
//                                    if (GUILayout.Button("Spawn " + ident.ToString()))
//                                    {
//                                        GameObject prefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(ident);
//                                        if (prefab != null)
//                                        {
//                                            for (int i = 0; i < spawnAmount; i++)
//                                            {
//                                                SRBehaviour.InstantiateActor(prefab, SRSingleton<SceneContext>.Instance.GameModel.player.currRegionSetId, SRSingleton<SceneContext>.Instance.GameModel.player.GetPos() + new Vector3(UnityEngine.Random.Range(-1f, 1f), 4f + (0.1f * (float)i), UnityEngine.Random.Range(-1f, 1f)), Quaternion.identity, false);
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                        catch { }
//                    }
//                    GUILayout.EndScrollView();
//                }
//                else if (cheatMenuType == 1)
//                {
//                    foreach (var player in Globals.Players.Values)
//                    {
//                        if (!player.IsLocal)
//                        {
//                            if (GUILayout.Button("Teleport to " + player))
//                            {
//                                SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(player.CurrentRegionSet);
//                                SRSingleton<SceneContext>.Instance.player.transform.position = player.transform.position + new Vector3(0, 2, 0);
//                            }
//                        }
//                    }
//                }
//                else if (cheatMenuType == 2)
//                {
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var ident in (ProgressDirector.ProgressType[])Enum.GetValues(typeof(ProgressDirector.ProgressType)))
//                    {
//                        try
//                        {
//                            if (ident.ToString().Contains("UNLOCK"))
//                            {
//                                if (GUILayout.Button(ident.ToString()))
//                                {
//                                    SRSingleton<SceneContext>.Instance.ProgressDirector.AddProgress(ident);
//                                }
//                            }
//                        }
//                        catch { }
//                    }
//                    GUILayout.EndScrollView();
//                }
//                else if (cheatMenuType == 3)
//                {
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var ident in (ExchangeDirector.OfferType[])Enum.GetValues(typeof(ExchangeDirector.OfferType)))
//                    {
//                        try
//                        {
//                            if (GUILayout.Button("Clear " + ident.ToString()))
//                            {
//                                SRSingleton<SceneContext>.Instance.ExchangeDirector.ClearOffer(ident);
//                            }
//                        }
//                        catch { }
//                    }
//                    GUILayout.EndScrollView();
//                }
//                else if (cheatMenuType == 4)
//                {
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var ginger in gingers)
//                    {
//                        if (ginger != null && ginger.bed.activeSelf && ginger.spawnJoint.connectedBody != null)
//                        {
//                            if (GUILayout.Button("Active Ginger " + ginger.spawnJoint.connectedBody.transform.position))
//                            {
//                                SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(ginger.GetComponentInParent<Region>(true).setId);
//                                SRSingleton<SceneContext>.Instance.player.transform.position = ginger.transform.position + new Vector3(0, 2, 0);
//                            }
//                        }
//                        else
//                        {
//                            //if (GUILayout.Button("Inactive Ginger " + ginger.id))
//                            //{
//                            //    SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(ginger.GetComponentInParent<Region>(true).setId);
//                            //    SRSingleton<SceneContext>.Instance.player.transform.position = ginger.transform.position + new Vector3(0, 2, 0);
//                            //}
//                        }
//                    }
//                    GUILayout.EndScrollView();
//                }
//                else if (cheatMenuType == 5)
//                {
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var kookadoba in kookadobas)
//                    {
//                        if (kookadoba != null && kookadoba.bed.activeSelf && kookadoba.spawnJoint.connectedBody != null)
//                        {
//                            if (GUILayout.Button("Active Kookadoba " + kookadoba.spawnJoint.connectedBody.transform.position))
//                            {
//                                SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(kookadoba.GetComponentInParent<Region>(true).setId);
//                                SRSingleton<SceneContext>.Instance.player.transform.position = kookadoba.transform.position + new Vector3(0, 2, 0);
//                            }
//                        }
//                        else
//                        {
//                            //if (GUILayout.Button("Inactive Ginger " + ginger.id))
//                            //{
//                            //    SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(ginger.GetComponentInParent<Region>(true).setId);
//                            //    SRSingleton<SceneContext>.Instance.player.transform.position = ginger.transform.position + new Vector3(0, 2, 0);
//                            //}
//                        }
//                    }
//                    GUILayout.EndScrollView();
//                }
//                else if (cheatMenuType == 6)
//                {
//                    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                    foreach (var site in PhaseSite.allSites)
//                    {
//                        if (site != null)
//                        {
//                            if (GUILayout.Button(site.id + " " + (site.phaseableObject != null)))
//                            {
//                                SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet(site.GetComponentInParent<Region>(true).setId);
//                                SRSingleton<SceneContext>.Instance.player.transform.position = site.transform.position + new Vector3(0, 2, 0);
//                            }
//                        }
//                    }
//                    GUILayout.EndScrollView();
//                }

//                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
//            }
//            else
//            {
//                if (lastCodeUse > 0)
//                {
//                    GUILayout.Label("Trying to connect using server code...");
//                }
//                else if (!string.IsNullOrWhiteSpace(LastError))
//                {
//                    GUILayout.Label("Error connecting:");
//                    GUILayout.Label(LastError);
//                    if (GUILayout.Button("OK"))
//                    {
//                        LastError = "";
//                    }
//                }
//                else
//                {
//                    GUILayout.Label("You can close this menu with F4");
//                    GUILayout.Space(20);

//                    if (!Globals.IsClient && !Globals.IsServer)
//                    {
//                        if (state == 0)
//                        {
//                            GUILayout.BeginHorizontal();
//                            GUILayout.Label("Username", GUILayout.Width(60));
//                            Globals.Username = GUILayout.TextField(Globals.Username, 30);
//                            GUILayout.EndHorizontal();

//                            if (string.IsNullOrWhiteSpace(Globals.Username) || !new Regex(@"^[a-zA-Z_][\w]*$").IsMatch(Globals.Username))
//                            {
//                                GUILayout.Label("Invalid username");
//                            }
//                            else
//                            {
//                                GUILayout.BeginHorizontal();
//                                if (GUILayout.Button("Continue"))
//                                {
//                                    state = 10;
//                                    SaveSettings();
//                                    SRSingleton<NetworkMasterServer>.Instance.UpdateName(Globals.Username);
//                                }
//                                //if (GUILayout.Button("Join Game"))
//                                //{
//                                //    state = 20;
//                                //    SaveSettings();
//                                //}
//                                GUILayout.EndHorizontal();
//                            }
//                        }
//                        else if (Levels.isMainMenu())
//                        {
//                            GUILayout.Label("Username: " + Globals.Username);
//                            if (GUILayout.Button("Change Username"))
//                            {
//                                state = 0;
//                            }

//                            if (SRSingleton<NetworkMasterServer>.Instance.Status == NetworkMasterServer.ConnectionStatus.Connected)
//                            {
//                                GUILayout.Space(20);

//                                GUILayout.Label("Join with server code:");
//                                GUILayout.BeginHorizontal();
//                                servercode = GUILayout.TextField(servercode, 5, GUILayout.Width(80));
//                                servercode = servercode.Replace(" ", "").ToUpper();
//                                if (GUILayout.Button("Join"))
//                                {
//                                    lastCodeUse = 5;
//                                    SRSingleton<NetworkMasterServer>.Instance.JoinServer(servercode);
//                                }
//                                GUILayout.EndHorizontal();
//                            }
//                            else
//                            {
//                                GUILayout.Label("Server codes currently not available");
//                            }

//                            GUILayout.Space(20);

//                            GUILayout.Label("Join with IP Address:");
//                            GUILayout.BeginHorizontal();
//                            GUILayout.Label("IP Address", GUILayout.Width(80));
//                            ipaddress = GUILayout.TextField(ipaddress);
//                            GUILayout.EndHorizontal();
//                            GUILayout.BeginHorizontal();
//                            GUILayout.Label("Port", GUILayout.Width(80));
//                            port = GUILayout.TextField(port);
//                            GUILayout.EndHorizontal();
//                            if (string.IsNullOrWhiteSpace(ipaddress))
//                            {
//                                GUILayout.Label("IPAddress invalid");
//                            }
//                            else if (!int.TryParse(port, out int numport) || numport < 1000 || numport > 65000)
//                            {
//                                GUILayout.Label("Invalid port");
//                            }
//                            else
//                            {
//                                if (GUILayout.Button("Connect"))
//                                {
//                                    SRSingleton<NetworkClient>.Instance.Connect(ipaddress, numport, Globals.Username);
//                                    SaveSettings();
//                                }
//                            }

//                            GUILayout.Space(20);
//                            GUILayout.Label("Games in the same Network:");
//                            if (GUILayout.Button("Refresh"))
//                            {
//                                SRSingleton<NetworkClient>.Instance.SendDiscoverMessage();
//                            }
//                            if (SRSingleton<NetworkClient>.Instance.LocalGames.Count == 0)
//                            {
//                                GUILayout.Label("No games found");
//                            }
//                            foreach (var game in SRSingleton<NetworkClient>.Instance.LocalGames)
//                            {
//                                if (GUILayout.Button(game.Name))
//                                {
//                                    SRSingleton<NetworkClient>.Instance.Connect(game.IP, game.Port, Globals.Username);
//                                }
//                            }
//                        }
//                        else if (!Levels.isSpecial())
//                        {
//                            GUILayout.Label("Username: " + Globals.Username);
//                            if (GUILayout.Button("Change Username"))
//                            {
//                                state = 0;
//                            }
//                            GUILayout.Space(20);

//                            //if (selectSave)
//                            //{
//                            //    saveScroll = GUILayout.BeginScrollView(saveScroll);
//                            //    foreach (var save in saveFolders)
//                            //    {
//                            //        if (GUILayout.Button(save))
//                            //        {
//                            //            saveName = save;
//                            //        }
//                            //    }
//                            //    GUILayout.EndScrollView();
//                            //    GUILayout.Label("Selected save: " + saveName);
//                            //    if (GUILayout.Button("Create new"))
//                            //    {
//                            //        selectSave = false;
//                            //    }
//                            //}
//                            //else
//                            //{
//                            //    GUILayout.BeginHorizontal();
//                            //    GUILayout.Label("Save Name", GUILayout.Width(80));
//                            //    saveName = GUILayout.TextField(saveName);
//                            //    GUILayout.EndHorizontal();
//                            //    isSharedCurrency = GUILayout.Toggle(isSharedCurrency, "Shared Currency");
//                            //    isSharedUpgrades = GUILayout.Toggle(isSharedUpgrades, "Shared Upgrades");
//                            //    if (saveFolders.Count > 0)
//                            //    {
//                            //        if (GUILayout.Button("Select existing save"))
//                            //        {
//                            //            selectSave = true;
//                            //        }
//                            //    }
//                            //}
//                            GUILayout.BeginHorizontal();
//                            GUILayout.Label("Port", GUILayout.Width(80));
//                            port = GUILayout.TextField(port);
//                            GUILayout.EndHorizontal();
//                            isPublicServer = GUILayout.Toggle(isPublicServer, "Show in Discord");

//                            if (!int.TryParse(port, out int numport) || numport < 1000 || numport > 65000)
//                            {
//                                GUILayout.Label("Invalid port");
//                            }
//                            //else if (!Utils.IsValidPath(saveName))
//                            //{
//                            //    GUILayout.Label("Invalid save name");
//                            //}
//                            else
//                            {
//                                if (GUILayout.Button("Host"))
//                                {
//                                    NetworkServer.Instance.StartServer(numport);
//                                    SaveSettings();
//                                }
//                            }
//                        }
//                    }

//                    if (Globals.IsMultiplayer)
//                    {
//                        QuicksilverEnergyGenerator generator = null;
//                        foreach (var region in Globals.LocalPlayer.Regions)
//                        {
//                            generator = region.GetComponent<QuicksilverEnergyGenerator>();
//                            if (generator != null)
//                            {
//                                break;
//                            }
//                        }
//                        GUILayout.Label("Is In Race: " + (generator != null ? generator.id : "None"));
//                        GUILayout.Label($"Client Status: {NetworkClient.Instance.Status}");
//                        GUILayout.Label($"Server Status: {NetworkServer.Instance.Status}");
//                        if (!string.IsNullOrWhiteSpace(Globals.ServerCode))
//                        {
//                            GUILayout.Label($"Server Code: {Globals.ServerCode}");
//                        }

//                        GUILayout.Label("Received Messages: " + SRSingleton<NetworkClient>.Instance.Statistics.ReceivedMessages);
//                        GUILayout.Label("Send Messages: " + SRSingleton<NetworkClient>.Instance.Statistics.SentMessages);

//                        if (NetworkServer.Instance != null && NetworkServer.Instance.Status == NetworkServer.ServerStatus.Running)
//                        {
//                            GUILayout.Label("Received Bytes: " + Utils.GetHumanReadableFileSize(NetworkServer.Instance.Statistics.ReceivedBytes));
//                            GUILayout.Label("Sent Bytes: " + Utils.GetHumanReadableFileSize(NetworkServer.Instance.Statistics.SentBytes));
//                        }
//                        else
//                        {
//                            GUILayout.Label("Received Bytes: " + Utils.GetHumanReadableFileSize(NetworkClient.Instance.Statistics.ReceivedBytes));
//                            GUILayout.Label("Sent Bytes: " + Utils.GetHumanReadableFileSize(NetworkClient.Instance.Statistics.SentBytes));
//                        }
//                        //GUILayout.Space(20);
//                        //GUILayout.Label("Chat");
//                        chatScroll = GUILayout.BeginScrollView(chatScroll, GUI.skin.box);
//                        foreach (var sizes in Globals.PacketSize.OrderByDescending(c => c.Value))
//                        {
//                            GUILayout.Label(sizes.Key + ": " + Utils.GetHumanReadableFileSize(sizes.Value));
//                        }
//                        //var skin = GUI.skin.box;
//                        //skin.wordWrap = true;
//                        //skin.alignment = TextAnchor.MiddleLeft;
//                        //foreach (var msg in chatMessages)
//                        //{
//                        //    GUILayout.Label(wrapString(msg, 250), skin, GUILayout.MaxWidth(250));
//                        //}
//                        GUILayout.EndScrollView();
//                    }
//                }

//                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
//            }
//        }

//        string wrapString(string msg, int width)
//        {
//            string[] words = msg.Split(" "[0]);
//            string retVal = ""; //returning string 
//            string NLstr = "";  //leftover string on new line
//            for (int index = 0; index < words.Length; index++)
//            {
//                string word = words[index].Trim();
//                //if word exceeds width
//                if (words[index].Length >= width + 2)
//                {
//                    string[] temp = new string[5];
//                    int i = 0;
//                    while (words[index].Length > width)
//                    { //word exceeds width, cut it at widrh
//                        temp[i] = words[index].Substring(0, width) + "\n"; //cut the word at width
//                        words[index] = words[index].Substring(width);     //keep remaining word
//                        i++;
//                        if (words[index].Length <= width)
//                        { //the balance is smaller than width
//                            temp[i] = words[index];
//                            NLstr = temp[i];
//                        }
//                    }
//                    retVal += "\n";
//                    for (int x = 0; x < i + 1; x++)
//                    { //loops through temp array
//                        retVal = retVal + temp[x];
//                    }
//                }
//                else if (index == 0)
//                {
//                    retVal = words[0];
//                    NLstr = retVal;
//                }
//                else if (index > 0)
//                {
//                    if (NLstr.Length + words[index].Length <= width)
//                    {
//                        retVal = retVal + " " + words[index];
//                        NLstr = NLstr + " " + words[index]; //add the current line length
//                    }
//                    else if (NLstr.Length + words[index].Length > width)
//                    {
//                        retVal = retVal + "\n" + words[index];
//                        NLstr = words[index]; //reset the line length
//                        print("newline! at word " + words[index]);
//                    }
//                }
//            }
//            return retVal;
//        }

//        private void SaveSettings()
//        {
//            PlayerPrefs.SetString("username", Globals.Username);
//            PlayerPrefs.SetString("ipaddress", ipaddress);
//            PlayerPrefs.SetString("port", port);
//            PlayerPrefs.SetString("gamename", saveName);
//        }
//    }
//}