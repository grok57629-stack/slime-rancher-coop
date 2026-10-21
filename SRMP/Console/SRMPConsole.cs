using JetBrains.Annotations;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

namespace SRMultiplayer
{
    enum LogItems
    {
        CLIENT,
        SERVER,
        PLAYERAMMO,
        EXCHANGE
    }


    public class SRMPConsole : MonoBehaviour
    {
        ConsoleWindow console = new ConsoleWindow();
        ConsoleInput input = new ConsoleInput();

        //
        // Create console window, register callbacks
        //
        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            console.Initialize();
            console.SetTitle("Slime Rancher");

            input.OnInputText += OnInputText;

            Application.logMessageReceived += Application_logMessageReceived;

            SRMP.Log("Console Started");
        }

        //used to prevent duplicate messages displaying and list what number duplicate it is 
        string LastMessage = "";
        int duplicateCount = 0;

        //types of console types that can be enabled/disabled
        //server automatically starts with all active
        List<LogType> blockMessages = new List<LogType>(); //keeps a list of message types that have been disabled
        List<LogItems> blockLogs = new List<LogItems>(); //keeps a list of log message types that have been disabled
        bool DisplayTrace = false;
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            // We're half way through typing something, so clear this line ..
            if (Console.CursorLeft != 0)
                input.ClearLine();

            //construct message
            string message = condition;
            if (!string.IsNullOrEmpty(stackTrace) && DisplayTrace)
            {
                //add stack strace if included
                message += Environment.NewLine + stackTrace;
            }

            if (message == LastMessage)
            {
                //do not process duplicate marks if the last item was not written
                if (duplicateCount > 0) duplicateCount++;
            }
            else
            {
                //add write line for duplicate notices if necessary
                if (duplicateCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Output Duplicated: " + duplicateCount);
                }

                //format color for message 
                if (type == LogType.Warning)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (type == LogType.Error)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.White;

                // mark new message
                LastMessage = message;

                bool displayLog = true;

                //if log type is blocked turn off display
                if (blockMessages.Contains(type)) displayLog = false;

                //check data for specific inner types 
                string data = condition;

                //remove the srmp tag and the time to check inner tags 
                if (type == LogType.Log && data.StartsWith("[SRMP]"))
                {
                    data = data.Substring(17);

                    //if is still able to display make sure the log message blocker isnt blocking it
                    if (displayLog)
                    {
                        //try to pase the log message
                        if (Enum.TryParse(data.Split("]"[0])[0].Substring(1), true, out LogItems logMessage))
                        {
                            if (blockLogs.Contains(logMessage)) displayLog = false;
                        }
                    }
                }


                //always allow console reply to display
                if (data.StartsWith("[Console]")) displayLog = true;

                //only write the message type if its not blocked             
                if (displayLog)
                {
                    //write the new line if not blocked                  
                    duplicateCount = 0;
                    Console.WriteLine(message);
                }
                else
                {
                    //for testing log disabled display
                    //Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(type.ToString() + " Dismissed");

                    //mark dupilcate count to -1
                    //prevent duplicate count for supressed messages from displaying
                    duplicateCount = -1;
                }
            }



            // If we were typing something re-add it.
            input.RedrawInputLine();
        }

        //create a log call that marks all console sent replys
        void ConsoleLog(string message)
        {
            SRMP.Log(message, "Console");
        }



        //
        // Text has been entered into the console
        // Run it as a console command
        //
        void OnInputText(string obj)
        {
            var args = obj.Split(' ');
            var cmd = args.FirstOrDefault();
            args = args.Skip(1).ToArray();

            switch (cmd.ToLower())
            {
                case "cheat":
                    {
                        if (args.Length > 0)
                        {
                            if (args[0].Equals("money"))
                            {
                                if (args.Length != 2 || !int.TryParse(args[1], out int money))
                                {
                                    ConsoleLog("Usage: cheat money <amount>");
                                }
                                else
                                {
                                    if (money > 0)
                                        SRSingleton<SceneContext>.Instance.PlayerState.AddCurrency(money);
                                    else
                                        SRSingleton<SceneContext>.Instance.PlayerState.SpendCurrency(money);
                                }
                            }
                            else if (args[0].Equals("enable"))
                            {
                                //TestUI.Instance.cheat = !TestUI.Instance.cheat;
                            }
                            else if (args[0].Equals("keys"))
                            {
                                if (args.Length != 2 || !int.TryParse(args[1], out int value))
                                {
                                    ConsoleLog("Usage: cheat keys <amount>");
                                }
                                else
                                {
                                    SRSingleton<SceneContext>.Instance.PlayerState.model.keys = value;
                                }
                            }
                            else if (args[0].Equals("allgadgets"))
                            {
                                foreach (var data in (Gadget.Id[])Enum.GetValues(typeof(Gadget.Id)))
                                {
                                    SRSingleton<SceneContext>.Instance.GadgetDirector.model.gadgets[data] = int.MaxValue;
                                }
                                foreach (var data in Identifiable.CRAFT_CLASS.Union(Identifiable.PLORT_CLASS))
                                {
                                    SRSingleton<SceneContext>.Instance.GadgetDirector.model.craftMatCounts[data] = int.MaxValue;
                                }
                            }
                            else if (args[0].Equals("spawn"))
                            {
                                if (args.Length == 2)
                                {
                                    if (Enum.TryParse<Identifiable.Id>(args[1].ToUpper(), out Identifiable.Id id))
                                    {
                                        GameObject prefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(id);
                                        if (prefab != null)
                                        {
                                            SRBehaviour.InstantiateActor(prefab, SRSingleton<SceneContext>.Instance.GameModel.player.currRegionSetId, SRSingleton<SceneContext>.Instance.GameModel.player.GetPos() + new Vector3(0, 4, 0), Quaternion.identity, false);
                                        }
                                        else
                                        {
                                            ConsoleLog(id + " can not be spawned");
                                        }
                                    }
                                    else
                                    {
                                        var data = Enum.GetNames(typeof(Identifiable.Id)).Where(n => n.ToLower().Contains(args[1].ToLower()));
                                        SRMP.Log(args[1] + " not found. " + (data.Count() > 0 ? " Did you mean one of these?" : ""));
                                        foreach (var name in data)
                                        {
                                            ConsoleLog(name);
                                        }
                                    }
                                }
                                else if (args.Length == 3 && int.TryParse(args[2], out int amount))
                                {
                                    if (Enum.TryParse<Identifiable.Id>(args[1].ToUpper(), out Identifiable.Id id))
                                    {
                                        GameObject prefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(id);
                                        if (prefab != null)
                                        {
                                            for (int i = 0; i < amount; i++)
                                            {
                                                SRBehaviour.InstantiateActor(prefab, SRSingleton<SceneContext>.Instance.GameModel.player.currRegionSetId, SRSingleton<SceneContext>.Instance.GameModel.player.GetPos() + new Vector3(0, 4, 0), Quaternion.identity, false);
                                            }
                                        }
                                        else
                                        {
                                            SRMP.Log(id + " can not be spawned");
                                        }
                                    }
                                    else
                                    {
                                        var data = Enum.GetNames(typeof(Identifiable.Id)).Where(n => n.ToLower().Contains(args[1].ToLower()));
                                        ConsoleLog(args[1] + " not found. " + (data.Count() > 0 ? " Did you mean one of these?" : ""));
                                        foreach (var name in data)
                                        {
                                            ConsoleLog(name);
                                        }
                                    }
                                }
                                else
                                {
                                    ConsoleLog("Usage: cheat spawn <id> (<amount>)");
                                }
                            }
                        }
                        else
                        {
                            ConsoleLog("Available sub commands:");
                            ConsoleLog("cheat money <amount>");
                            ConsoleLog("cheat keys <amount>");
                            ConsoleLog("cheat spawn <id> (<amount>)");
                            ConsoleLog("cheat allgadgets");
                        }
                    }
                    break;
                case "tp":
                    {
                        if (args.Length < 1 || args.Length > 2)
                        {
                            ConsoleLog("Usage: tp <(optional)target:username> <destination>");
                            return;
                        }

                        //mark target and destination seperately
                        string target = args.Length == 2 ? args[0] : "";
                        string destination = args.Length == 2 ? args[1] : args[0];

                        //mark target transform location
                        PacketPlayerPosition packet = null;
                        string destSummary = "";

                        //first check distination
                        switch (destination.ToLower())
                        {
                            case "home":

                                var home = SRSingleton<SceneContext>.Instance.GetWakeUpDestination();
                                packet = new PacketPlayerPosition()
                                {
                                    Position = home.transform.position,
                                    Rotation = home.transform.eulerAngles.y,
                                    RegionSet = (byte)home.GetRegionSetId()
                                };

                                destSummary = "Home";
                                break;
                            default: //check for a player name
                                var play = Globals.Players.Values.FirstOrDefault(p => p.Username.Equals(destination, StringComparison.CurrentCultureIgnoreCase));

                                if (play == null)
                                {
                                    ConsoleLog("Destination not found");
                                    return;
                                }
                                packet = new PacketPlayerPosition()
                                {
                                    Position = play.transform.position,
                                    Rotation = play.transform.eulerAngles.y,
                                    RegionSet = (byte)play.CurrentRegionSet
                                };
                                destSummary = play.Username;
                                break;
                        }


                        if (packet != null)
                        {
                            //set as not load destination
                            packet.OnLoad = false;

                            NetworkPlayer targetPlayer = null;
                            if (target.Length > 0)
                            {
                                targetPlayer = Globals.Players.Values.FirstOrDefault(p => p.Username.Equals(target, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                targetPlayer = Globals.LocalPlayer;
                            }
                            //check if target is local user
                            if (targetPlayer == null)
                            {
                                ConsoleLog("Target Player not found");
                                return;
                            }

                            ConsoleLog("Teleporting " + targetPlayer.Username + " to " + destSummary);

                            if (!targetPlayer.IsLocal)
                            {
                                //if a target is located and is not the local player send the teleport command
                                packet.ID = targetPlayer.ID;
                                packet.WeaponY = targetPlayer.GetWeaponLocation();
                                packet.Send();
                                return;
                            }
                            else
                            {
                                //if we have have made it here the tp in question is for the instances player 
                                //so move that player
                                SRSingleton<SceneContext>.Instance.player.transform.position = packet.Position;
                                SRSingleton<SceneContext>.Instance.player.transform.eulerAngles = new Vector3(0, packet.Rotation, 0);
                                SRSingleton<SceneContext>.Instance.PlayerState.model.SetCurrRegionSet((RegionRegistry.RegionSetId)packet.RegionSet);
                                // play the teleport animation
                                SRSingleton<Overlay>.Instance.PlayTeleport();
                            }

                        }
                        else
                        {
                            ConsoleLog("Destination not found");
                        }

                    }
                    break;
                case "listplayers":
                    {
                        ConsoleLog("Players:");
                        foreach (var player in Globals.Players.Values)
                        {
                            SRMP.Log(player.Username);
                        }
                    }
                    break;
                case "sleep":
                    {
                        if (args.Length == 1)
                        {
                            SRSingleton<SceneContext>.Instance.TimeDirector.FastForwardTo(SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(float.Parse(args[0])));
                            ConsoleLog("Sleeoing for " + args[0] + " hours");
                        }
                        else
                        {
                            ConsoleLog("Usage: sleep <hours>");
                        }
                    }
                    break;
                case "console": //add toggle option for turning on and off logging types

                    if (args.Length > 1 && (args[0] == "enable" || args[0] == "disable"))
                    {
                        bool enable = args[0] == "enable";
                        //double check type
                        if (Enum.TryParse(args[1], true, out LogType logType)) //check for main log type
                        {
                            if (enable)
                            {
                                if (!blockMessages.Contains(logType)) blockMessages.Remove(logType);
                                ConsoleLog(logType.ToString() + " Messages Enabled");
                            }
                            else
                            {
                                if (blockMessages.Contains(logType)) blockMessages.Add(logType);
                                ConsoleLog("[Console] " + logType.ToString() + " Messages Disabled");
                            }
                        }
                        else if (Enum.TryParse(args[1], true, out LogItems logMessage)) //check for log message item types
                        {
                            if (enable)
                            {
                                if (!blockLogs.Contains(logMessage)) blockLogs.Remove(logMessage);
                                ConsoleLog(logMessage.ToString() + " Log Messages Enabled");
                            }
                            else
                            {
                                if (blockLogs.Contains(logMessage)) blockLogs.Add(logMessage);
                                ConsoleLog(logMessage.ToString() + " Log Messages Disabled");
                            }
                        }
                        else if (args[1].Equals("stacktrace", StringComparison.InvariantCultureIgnoreCase) || args[1].Equals("stack_trace", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (enable)
                            {
                                DisplayTrace = true;
                                ConsoleLog("Stack Trace Information Enabled");
                            }
                            else
                            {
                                DisplayTrace = false;
                                ConsoleLog("Stack Trace Information Disabled");
                            }
                        }
                        else
                        {
                            ConsoleLog("Invalid Feed back Type");
                            ConsoleLog("Valid Types: ");
                            ConsoleLog(string.Join(", ", Enum.GetValues(typeof(LogType))));
                            ConsoleLog(string.Join(", ", Enum.GetValues(typeof(LogItems))));
                        }
                    }
                    else
                    {
                        ConsoleLog("Usage: console <enable/disable> <feedbackType>");
                    }
                    break;

            }
        }

        //
        // Update the input every frame
        // This gets new key input and calls the OnInputText callback
        //
        void Update()
        {
            input.Update();
        }

        //
        // It's important to call console.ShutDown in OnDestroy
        // because compiling will error out in the editor if you don't
        // because we redirected output. This sets it back to normal.
        //
        void OnDestroy()
        {
            console.Shutdown();
        }
    }
}