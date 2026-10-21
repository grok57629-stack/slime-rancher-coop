using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SRMultiplayer.Enums;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using UnityEngine;
using Object = System.Object;

namespace SRMultiplayer.Networking
{
    public class NetworkServer : EpicP2P
    {
        public static NetworkServer Instance { get; private set; }

        public ServerStatus status;
        
        public Dictionary<ProductUserId, byte> players = new Dictionary<ProductUserId, byte>();

        private readonly Dictionary<ProductUserId, float> closingPeers = new Dictionary<ProductUserId, float>();
        private readonly Dictionary<int, HashSet<byte>> regionPlayers = new Dictionary<int, HashSet<byte>>();
        private readonly Dictionary<int, byte> regionOwners = new Dictionary<int, byte>();
        private readonly HashSet<int> unavailableRegionWarnings = new HashSet<int>();
        
        public enum ServerStatus
        {
            Stopped,
            Running
        }
        public NetworkServer(P2PInterface p2PInterface) : base(p2PInterface, true)
        {
            Instance = this;
        }
        
        public void StartListen()
        {
            // EOS SRMP Stuff
            SetupP2P();
            SRSingleton<EpicApplication>.Instance.Metrics.BeginSession();

            // Original SRMP Stuff
            status = ServerStatus.Running;
            Utils.ActorSequence = 0;
            regionPlayers.Clear();
            regionOwners.Clear();
            unavailableRegionWarnings.Clear();

            Globals.DisableAchievements = true;
            Globals.PartyID = Guid.NewGuid();

            byte id = 1;
            while (id < 255 && Globals.Players.Values.Any(p => p.ID == id))
                id++;

            Globals.LocalID = id;
            Globals.LocalPlayer = SRSingleton<SceneContext>.Instance.Player.AddComponent<NetworkPlayer>();
            Globals.LocalPlayer.ID = id;
            Globals.LocalPlayer.Username = Globals.Username;
            Globals.LocalPlayer.HasLoaded = true;
            var _ = Globals.Mods; // Make sure mods list is set up to check for vr
            Globals.LocalPlayer.IsVR = Globals.VRInstalled;
            Globals.LocalPlayer.Spawn();
            Globals.Players.Add(id, Globals.LocalPlayer);
            Globals.PlayerToEpic.Add(id, EpicApplication.Instance.Authentication.ProductUserId);
            Globals.EpicToPlayer.Add(EpicApplication.Instance.Authentication.ProductUserId, id);
            Globals.ClientLoaded = true;

            var worldName = SRSingleton<GameContext>.Instance.AutoSaveDirector.SavedGame.GetName();
            Directory.CreateDirectory(Path.Combine(SRMP.ModDataPath, worldName));

            //bans belong to the world, so they follow whichever save is hosted
            Server.BanList.LoadForWorld(worldName);

            foreach(var netRegion in Globals.Regions.Values)
            {
                if(netRegion.Region.root.activeInHierarchy)
                {
                    netRegion.AddPlayer(Globals.LocalPlayer);
                    netRegion.TakeOwnership();
                }
            }
            foreach(var actor in SRSingleton<SceneContext>.Instance.GameModel.AllActors().Values)
            {
                if (actor.ident != Identifiable.Id.NONE && actor.ident != Identifiable.Id.PLAYER && !Identifiable.SCENE_OBJECTS.Contains(actor.ident))
                {
                    var netActor = actor.transform.gameObject.AddComponent<NetworkActor>();
                    netActor.ID = Utils.GetRandomActorID();
                    netActor.Ident = (ushort)actor.ident;
                    netActor.RegionSet = (byte)actor.currRegionSetId;
                    if (actor.transform.gameObject.activeInHierarchy)
                    {
                        netActor.Owner = Globals.LocalID;
                    }

                    Globals.Actors.Add(netActor.ID, netActor);
                }
            }
            foreach(var landPlot in SRSingleton<SceneContext>.Instance.GameModel.AllLandPlots().Values)
            {
                var netLandPlot = landPlot.gameObj.GetComponent<NetworkLandplot>();
                netLandPlot.Plot = landPlot.gameObj.GetComponentInChildren<LandPlot>(true);
            }
            foreach (TutorialDirector.Id tut in (TutorialDirector.Id[])Enum.GetValues(typeof(TutorialDirector.Id)))
            {
                SRSingleton<SceneContext>.Instance.TutorialDirector.tutModel.completedIds.Add(tut);
            }
        }

        public void SendPacket(ProductUserId targetUserId, IPacket packet,
            PacketReliability packetReliability = PacketReliability.ReliableOrdered)//, byte channel = 0)
        {
            if (packet == null || status != ServerStatus.Running) return;
            var message = new NetOutgoingMessage();
            packet.Serialize(message);
            SendMessage(targetUserId, packet.GetPacketType(), message, packetReliability);
        }

        public void SendPacketToAll(IPacket packet, ProductUserId except = null, PacketReliability packetReliability = PacketReliability.ReliableOrdered)//, byte channel = 0)
        {
            var self = EpicApplication.Instance.Authentication.ProductUserId;
            foreach (var player in Globals.EpicToPlayer.Keys.ToList())
            {
                if (except == player || player == self) continue;
                if (!Globals.Players.TryGetValue(Globals.EpicToPlayer[player], out var member) ||
                    member == null || member.State != NetworkPlayerState.Connected) continue;
                var type = packet.GetPacketType();
                if (!member.HasLoaded && type != PacketType.PlayerJoined && type != PacketType.PlayerLeft && type != PacketType.PlayerLoaded)
                    continue;

                SendPacket(player, packet, packetReliability);
            }
        }

        public override void OnMessageReceived(ProductUserId senderUserId, byte channel, ref NetIncomingMessage im)
        {
            if (!Globals.EpicToPlayer.TryGetValue(senderUserId, out byte id) ||
                !Globals.Players.TryGetValue(id, out var player) || player == null) return;
            if (!TryReadMessage(senderUserId, ref im, out PacketType type)) return;
            if (player.State == NetworkPlayerState.Authenticating)
            {
                if (type == PacketType.Authentication) HandleAuthentication(player, im);
                return;
            }
            if (player.State == NetworkPlayerState.Connected && type != PacketType.Authentication)
                NetworkHandlerServer.HandlePacket(type, im, player);
        }
        
        public override void OnConnected(ProductUserId remoteUserId, NetworkConnectionType networkType, ConnectionEstablishedType connectionType)
        {
            // EOS also emits established when an interrupted connection recovers.
            // It is still the same player, not a second authentication/session.
            if (status != ServerStatus.Running || Globals.EpicToPlayer.ContainsKey(remoteUserId)) return;
            if (Globals.Players.Count >= Globals.MaxPlayers)
            {
                CloseConnection(remoteUserId);
                return;
            }
            byte nextId = (byte)Enumerable.Range(2, 254).FirstOrDefault(id => !Globals.Players.ContainsKey((byte)id));
            if (nextId == 0) { CloseConnection(remoteUserId); return; }
            Globals.PlayerToEpic.Add(nextId, remoteUserId);
            Globals.EpicToPlayer.Add(remoteUserId, nextId);
            var playerObj = new GameObject("Player" + nextId);
            var player = playerObj.AddComponent<NetworkPlayer>();
            player.ID = nextId;
            Globals.Players.Add(nextId, player);
            player.State = NetworkPlayerState.Authenticating;
        }
        
        private void HandleAuthentication(NetworkPlayer player, NetIncomingMessage im)
        {
            var pid = player.ID;
            
            var username = im.ReadString();
            //Older clients send a local profile Guid here. Consume the field to
            //keep the handshake layout, but never trust it for host-side saves.
            im.ReadBytes(16);
            var build = im.ReadInt32();
            
            List<string> mods = new List<string>();
            var modsLen = im.ReadInt32();
            if (modsLen < 0 || modsLen > 4096) throw new InvalidDataException("Invalid mod count.");
            for (var i = 0; i < modsLen; i++)
                mods.Add(im.ReadString());

            var vr = im.ReadBoolean();
            int dlcCount = im.ReadInt32();
            if (dlcCount < 0 || dlcCount > 256) throw new InvalidDataException("Invalid DLC count.");
            var dlcs = new HashSet<DLCPackage.Id>();
            for (int i = 0; i < dlcCount; i++) dlcs.Add((DLCPackage.Id)im.ReadByte());
            var hostDlcs = new HashSet<DLCPackage.Id>(SRSingleton<GameContext>.Instance.DLCDirector.Installed);
            player.DLCs = dlcs.ToList();
            SRMP.Log($"[Join] {username} DLCs: {string.Join(", ", dlcs)}; host: {string.Join(", ", hostDlcs)}");
            if (Globals.UserData.CheckDLC && !hostDlcs.SetEquals(dlcs))
            {
                hostDlcs.SymmetricExceptWith(dlcs);
                DisconnectDLCMismatch(player, string.Join(", ", hostDlcs));
                return;
            }
            
            //checked before anything else accepts the player: the EOS id is
            //observed by us during the handshake, so it cannot be forged by
            //renaming or by editing client-side files
            if (!Globals.PlayerToEpic.TryGetValue(pid, out var authenticatedProductUserId)
                || authenticatedProductUserId == null)
            {
                DisconnectCustom(player, "The authenticated EOS identity is unavailable.");
                return;
            }
            if (Server.BanList.IsBanned(authenticatedProductUserId.ToString()))
            {
                var ban = Server.BanList.Find(authenticatedProductUserId.ToString());
                SRMP.Log($"[Bans] Refused banned player {username} ({authenticatedProductUserId})");
                DisconnectCustom(player, $"You are banned from this world: {ban.Reason}");
                return;
            }

            if (build != Globals.Version)
            {
                SRMP.Log($"Version Mismatch! YOU({Globals.Version}) vs PLAYER({build})");
                DisconnectVersionMismatch(player);
                return;
            }

            var modsDiff1 = Globals.Mods.Where(x => !mods.Contains(x));
            var modsDiff2 = mods.Where(x => !Globals.Mods.Contains(x));
            if (modsDiff1.Count() != 0 || modsDiff2.Count() != 0)
            {
                var modsDiff3 = modsDiff1.Union(modsDiff2);
                DisconnectModMismatch(player, string.Join(", ", modsDiff3.ToArray()));
                return;
            }
            
            new PacketPlayerJoined()
            {
                ID = pid,
                Username = username,
                VR = vr
            }.SendToAllExcept(player, NetDeliveryMethod.ReliableOrdered);
            
            player.UUID = NetworkIdentity.ForPlayerSave(authenticatedProductUserId.ToString());
            player.Username = username;
            player.name = $"{username} ({pid})";
            player.IsVR = vr;

            NetOutgoingMessage hail = new NetOutgoingMessage();
            
            hail.Write(pid);
            int actorCursor = Globals.Actors.Keys.Where(id => (byte)((uint)id >> 24) == pid)
                .Select(id => id & 0xFFFFFF).DefaultIfEmpty(0).Max();
            hail.Write(actorCursor);
            var roster = Globals.Players.Values.Where(p => p != null &&
                (p.ID == pid || p.IsLocal || p.State == NetworkPlayerState.Connected)).ToList();
            hail.Write(roster.Count);
            foreach (var p in roster)
            {
                hail.Write(p.ID);
                hail.Write(p.Username);
                hail.Write(p.HasLoaded);
                hail.Write(p.IsVR);
            }
            hail.Write(Globals.PartyID.ToByteArray());
            hail.Write(Globals.UserData.UUID.ToByteArray());
            hail.Write((byte)SRSingleton<SceneContext>.Instance.GameModel.currGameMode);
            hail.Write(SRSingleton<GameContext>.Instance.AutoSaveDirector.SavedGame.GetName());
            
            SRMP.Log($"[Join] Authentication roster sent: build={Globals.Version}, player={username} ({pid}), roster={roster.Count}, actorCursor={actorCursor}, bytes={hail.LengthBytes}.");
            
            SendMessage(Globals.PlayerToEpic[pid], PacketType.Authentication, hail);

            player.State = NetworkPlayerState.Connected;
        }

        public override void OnDisconnected(ProductUserId remoteUserId, ConnectionClosedReason reason)
        {
            closingPeers.Remove(remoteUserId);
            if (Globals.EpicToPlayer.TryGetValue(remoteUserId, out var player))
            {
                ForgetPeer(remoteUserId);
                ReclaimPlayerOwnership(player);
                foreach (var actor in Globals.Actors.Values)
                    if (actor != null) actor.KnownPlayers.RemoveAll(p => p == null || p.ID == player);

                if (Globals.Players.TryGetValue(player, out var leaving) && leaving != null)
                {
                    //the object outlives the dictionary entry otherwise, and stale
                    //entries linger in every region's player list
                    if (leaving.HasLoaded) leaving.Save();
                    foreach (var region in leaving.Regions.ToList())
                    {
                        region.RemovePlayer(leaving);
                    }
                    UnityEngine.Object.Destroy(leaving.gameObject);
                }

                Globals.Players.Remove(player);
                Globals.EpicToPlayer.Remove(remoteUserId);
                Globals.PlayerToEpic.Remove(player);
                
                new PacketPlayerLeft()
                {
                    ID = player
                }.SendToAll();
            }
        }

        /// <summary>
        /// Hands everything a departing player was simulating back to the host.
        /// Left alone these actors and regions stay owned by a player id that no
        /// longer exists, so nobody simulates them and nobody can claim them.
        /// </summary>
        private void ReclaimPlayerOwnership(byte leavingId)
        {
            int actors = 0;
            foreach (var actor in Globals.Actors.Values.ToList())
            {
                if (actor == null || actor.Owner != leavingId) continue;

                actor.SetOwnership(Globals.LocalID);
                actors++;

                new PacketActorOwner()
                {
                    ID = actor.ID,
                    Owner = Globals.LocalID
                }.SendToAll(NetDeliveryMethod.ReliableOrdered);
            }

            ForgetRegionPlayer(leavingId);
            var regionIds = regionOwners.Where(entry => entry.Value == leavingId).Select(entry => entry.Key)
                .Concat(Globals.Regions.Values.Where(region => region != null && region.Owner == leavingId)
                    .Select(region => region.ID))
                .Distinct()
                .ToList();
            int regions = 0;
            foreach (int regionId in regionIds)
            {
                Globals.Regions.TryGetValue(regionId, out var region);
                var connectedPlayers = regionPlayers.TryGetValue(regionId, out var present)
                    ? present.Where(id => Globals.Players.TryGetValue(id, out var candidate)
                        && candidate != null && candidate.HasLoaded
                        && (candidate.IsLocal || candidate.State == NetworkPlayerState.Connected))
                    : Enumerable.Empty<byte>();
                byte nextOwner = RegionOwnership.SelectReplacementOwner(Globals.LocalID, leavingId, connectedPlayers);

                TrackRegionOwner(regionId, nextOwner);
                region?.SetOwnership(nextOwner);
                regions++;

                new PacketRegionOwner()
                {
                    ID = regionId,
                    Owner = nextOwner
                }.SendToAll(NetDeliveryMethod.ReliableOrdered);

                SRMP.Log($"[Region] Owner handoff region={regionId} leaving={leavingId} next={nextOwner}");
            }

            if (actors > 0 || regions > 0)
            {
                SRMP.Log($"[Server] Reclaimed {actors} actor(s) and {regions} region(s) "
                         + $"from departing player {leavingId}");
            }
        }

        internal void TrackRegionPlayer(int regionId, byte playerId, bool present)
        {
            if (playerId == 0) return;
            if (present)
            {
                if (!regionPlayers.TryGetValue(regionId, out var players))
                    regionPlayers[regionId] = players = new HashSet<byte>();
                players.Add(playerId);
            }
            else if (regionPlayers.TryGetValue(regionId, out var players))
            {
                players.Remove(playerId);
                if (players.Count == 0) regionPlayers.Remove(regionId);
            }
        }

        private void ForgetRegionPlayer(byte playerId)
        {
            foreach (int regionId in regionPlayers.Keys.ToList())
                TrackRegionPlayer(regionId, playerId, false);
        }

        internal bool IsPlayerInRegion(int regionId, byte playerId)
        {
            return regionPlayers.TryGetValue(regionId, out var players) && players.Contains(playerId);
        }

        internal byte GetRegionOwner(int regionId, NetworkRegion region)
        {
            if (regionOwners.TryGetValue(regionId, out byte owner)) return owner;
            owner = region != null ? region.Owner : (byte)0;
            regionOwners[regionId] = owner;
            return owner;
        }

        internal void TrackRegionOwner(int regionId, byte owner)
        {
            regionOwners[regionId] = owner;
        }

        internal void LogUnavailableRegionOnce(int regionId, byte playerId)
        {
            if (unavailableRegionWarnings.Add(regionId))
                SRMP.Log($"[Region] Player {playerId} entered region {regionId}; host has no scene component, keeping remote membership for authority and handoff.");
        }

        public override void OnConnectionRequest(ProductUserId remoteUserId)
        {
            var localUserId = EpicApplication.Instance?.Authentication?.ProductUserId;
            if (localUserId != null && NetworkIdentity.IsSameProductUser(localUserId.ToString(), remoteUserId?.ToString()))
            {
                SRMP.Log("[Join] Rejected an EOS connection using the host's own Product User ID.");
                CloseConnection(remoteUserId);
                return;
            }

            if (EpicApplication.Instance.Lobby.ContainsUserId(remoteUserId))
            {
                AcceptConnection(remoteUserId);
            }
            else
            {
                CloseConnection(remoteUserId);
            }
        }

        public override void OnShutdown()
        {
            status = ServerStatus.Stopped;
            closingPeers.Clear();
            foreach (var peer in Globals.EpicToPlayer.Keys.ToList())
                if (peer != EpicApplication.Instance.Authentication.ProductUserId) CloseConnection(peer);
            EpicApplication.Instance?.Metrics?.EndSession();
        }

        protected override void OnLocalIdentityInvalidated(string reason)
        {
            SRMP.ReturnToMainMenu("Epic Online Services invalidated the host identity; sign in again before hosting (" + reason + ")");
        }

        private void AwaitClientDisconnect(NetworkPlayer player)
        {
            player.State = NetworkPlayerState.Disconnected;
            if (Globals.PlayerToEpic.TryGetValue(player.ID, out var peer))
                closingPeers[peer] = Time.realtimeSinceStartup + 5f;
        }

        protected override void OnTick()
        {
            // Give the reliable kick reason a chance to arrive. The client
            // closes on receipt; this timeout only reaps an unresponsive peer.
            foreach (var entry in closingPeers.ToList())
            {
                if (Time.realtimeSinceStartup < entry.Value) continue;
                CloseConnection(entry.Key);
                OnDisconnected(entry.Key, ConnectionClosedReason.ClosedByLocalUser);
            }
        }

        public void DisconnectKick(NetworkPlayer player)
        {
            new PacketKickClient()
            {
                reason = PacketKickClient.Reason.Kicked
            }.Send(player, NetDeliveryMethod.ReliableOrdered);
            
            AwaitClientDisconnect(player);
        }
        /// <summary>
        /// Disconnect player due to a mismatch between versions.
        /// </summary>
        public void DisconnectVersionMismatch(NetworkPlayer player)
        {
            new PacketKickClient()
            {
                reason = PacketKickClient.Reason.VersionMismatch,
                data = Globals.Version
            }.Send(player, NetDeliveryMethod.ReliableOrdered);
            
            AwaitClientDisconnect(player);
        }
        /// <summary>
        /// Disconnect player with a custom message.
        /// </summary>
        public void DisconnectCustom(NetworkPlayer player, string msg)
        {
            new PacketKickClient()
            {
                reason = PacketKickClient.Reason.Custom,
                data = msg
            }.Send(player, NetDeliveryMethod.ReliableOrdered);
            
            AwaitClientDisconnect(player);
        }
        /// <summary>
        /// Disconnect player due to different dlcs being installed
        /// </summary>
        public void DisconnectDLCMismatch(NetworkPlayer player, string list)
        {
            new PacketKickClient()
            {
                reason = PacketKickClient.Reason.DLCMismatch,
                data = list
            }.Send(player, NetDeliveryMethod.ReliableOrdered);
            
            AwaitClientDisconnect(player);
        }
        /// <summary>
        /// Disconnect player due to different mods being installed
        /// </summary>
        public void DisconnectModMismatch(NetworkPlayer player, string list)
        {
            new PacketKickClient()
            {
                reason = PacketKickClient.Reason.ModsMismatch,
                data = list
            }.Send(player, NetDeliveryMethod.ReliableOrdered);
            
            AwaitClientDisconnect(player);
        }
    }
}
