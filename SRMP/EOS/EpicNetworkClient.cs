using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SRMultiplayer.Enums;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SRMultiplayer.Networking
{
    public class NetworkClient : EpicP2P
    {
        private ProductUserId serverUserId;
        internal Guid HostUUID { get; private set; }
        private readonly Queue<Tuple<PacketType, NetIncomingMessage>> pendingMessages = new Queue<Tuple<PacketType, NetIncomingMessage>>();
        private int pendingBytes;
        private float authenticationDeadline;
        
        public static NetworkClient Instance { get; private set; }
        
        public NetworkClientStatus Status { get; private set; }

        public NetworkClient(P2PInterface p2PInterface) : base(p2PInterface, false)
        {
            Status = NetworkClientStatus.None;
            Instance = this;
        }

        public void Connect(ProductUserId serverUserId)
        {
            if (serverUserId == null)
            {
                SRMP.Log("Cannot connect: server user id is null.");
                Status = NetworkClientStatus.Disconnected;
                return;
            }

            Shutdown();
            this.serverUserId = serverUserId;
            Status = NetworkClientStatus.Connecting;
            authenticationDeadline = Time.realtimeSinceStartup + 30f;

            SetupP2P();

            AcceptConnection(serverUserId);
        }

        public void SendPacket(IPacket packet, PacketReliability packetReliability = PacketReliability.ReliableOrdered)//, byte channel = 0)
        {
            if (packet == null || serverUserId == null || Status == NetworkClientStatus.Disconnected || Status == NetworkClientStatus.None)
                return;

            var message = new NetOutgoingMessage();
            packet.Serialize(message);
            SendMessage(serverUserId, packet.GetPacketType(), message, packetReliability);
        }

        public override void OnMessageReceived(ProductUserId senderUserId, byte channel, ref NetIncomingMessage im)
        {
            if (serverUserId == null || senderUserId != serverUserId || Status == NetworkClientStatus.Disconnected)
                return;
            if (!TryReadMessage(senderUserId, ref im, out PacketType packetType)) return;
            SRMP.NoteServerContact();
            if (packetType == PacketType.Authentication)
            {
                if (Status != NetworkClientStatus.Authenticating) return;
                try
                {
                    im.m_readPosition = 0;
                    
                    Globals.LocalID = im.ReadByte();
                    Utils.ActorSequence = im.ReadInt32();

                    int playerCount = im.ReadInt32();
                    if (playerCount < 2 || playerCount > 255)
                    {
                        SRMP.Log($"[Join] Invalid authentication roster count={playerCount}, payloadBytes={im.LengthBytes}, localId={Globals.LocalID}; update the host and all clients to the same SRMP build.");
                        throw new System.IO.InvalidDataException("Invalid roster size.");
                    }
                    for (int i = 0; i < playerCount; i++)
                    {
                        byte id = im.ReadByte();
                        string username = im.ReadString() ?? $"Player {id}";
                        bool hasloaded = im.ReadBoolean();
                        bool isVR = im.ReadBoolean();

                        if (Globals.Players.TryGetValue(id, out var existingPlayer) && existingPlayer != null)
                        {
                            UnityEngine.Object.Destroy(existingPlayer.gameObject);
                            Globals.Players.Remove(id);
                        }

                        var playerObject = new GameObject($"{username} ({id})");
                        var player = playerObject.AddComponent<NetworkPlayer>();
                        UnityEngine.Object.DontDestroyOnLoad(playerObject);

                        player.ID = id;
                        player.Username = username;
                        player.HasLoaded = hasloaded;
                        player.IsVR = isVR;
                        Globals.Players[id] = player;

                        if (id == Globals.LocalID)
                        {
                            Globals.LocalPlayer = player;
                        }
                        if (isVR)
                            SRMP.Log($"Player {id} is a VR player!");
                    }
                    Globals.PartyID = new Guid(im.ReadBytes(16));
                    HostUUID = new Guid(im.ReadBytes(16));
                    var gameMode = (PlayerState.GameMode)im.ReadByte();
                    Globals.CurrentGameName = im.ReadString();
                    SRMP.Log("Auth Complete");
                    Status = NetworkClientStatus.Connected;

                    var gameContext = SRSingleton<GameContext>.Instance;
                    if (gameContext == null || gameContext.AutoSaveDirector == null)
                    {
                        SRMP.Log("Cannot finish multiplayer authentication: GameContext/AutoSaveDirector is not ready.");
                        SRMP.ReturnToMainMenu("authentication initialization failed");
                        return;
                    }

                    gameContext.AutoSaveDirector.LoadNewGame("SRMultiplayerGame", Identifiable.Id.GOLD_SLIME, gameMode, () =>
                    {
                        if (serverUserId != null)
                            CloseConnection(serverUserId);

                        SceneManager.LoadScene(2);
                    });
                }
                catch (Exception e)
                {
                    SRMP.Log($"Exception while applying authentication/join data:\n{e}");
                    SRMP.ReturnToMainMenu("invalid authentication data");
                }
            }
            else if (Status == NetworkClientStatus.Connected || packetType == PacketType.KickPlayer)
            {
                if (!Globals.GameLoaded && packetType != PacketType.KickPlayer)
                {
                    pendingBytes += im.LengthBytes;
                    if (pendingBytes > 32 * 1024 * 1024)
                    {
                        SRMP.ReturnToMainMenu("join packet queue exceeded its limit");
                        return;
                    }
                    pendingMessages.Enqueue(Tuple.Create(packetType, im));
                }
                else NetworkHandlerClient.HandlePacket(packetType, im);
            }
        }

        internal void DrainPendingMessages()
        {
            while (pendingMessages.Count > 0 && Status == NetworkClientStatus.Connected && Globals.GameLoaded)
            {
                var pending = pendingMessages.Dequeue();
                pendingBytes -= pending.Item2.LengthBytes;
                NetworkHandlerClient.HandlePacket(pending.Item1, pending.Item2);
            }
        }

        protected override void OnTick()
        {
            if ((Status == NetworkClientStatus.Connecting || Status == NetworkClientStatus.Authenticating) &&
                Time.realtimeSinceStartup >= authenticationDeadline)
                SRMP.ReturnToMainMenu("authentication timed out; verify that both peers use the same SRMP build");
        }

        protected override void OnLocalIdentityInvalidated(string reason)
        {
            SRMP.ReturnToMainMenu("Epic Online Services invalidated this connection; sign in again before rejoining (" + reason + ")");
        }

        public override void OnShutdown()
        {
            PlayerInventory.Save();
            pendingMessages.Clear();
            pendingBytes = 0;
            EpicApplication.Instance?.Metrics?.EndSession();

            if (serverUserId != null)
                CloseConnection(serverUserId);

            Status = NetworkClientStatus.Disconnected;
            ForgetPeer(serverUserId);
            serverUserId = null;

            //the lobby closing routes here rather than through OnDisconnected,
            //so without this the player is left standing in a world that is no
            //longer synchronised with anything
            if (SceneManager.GetActiveScene().buildIndex == 3)
            {
                Globals.GameLoaded = false;
                Globals.ClientLoaded = false;
                SceneManager.LoadScene(2);
            }
        }

        private void SendAuthentication()
        {
            if (serverUserId == null)
                return;

            NetOutgoingMessage om = new NetOutgoingMessage();
            om.Data = Array.Empty<byte>();

            
            om.Write(Globals.Username ?? "Player");
            om.Write(Globals.UserData.UUID.ToByteArray());
            om.Write(Globals.Version);

            var mods = Globals.Mods;
            
            om.Write(mods.Count);
            foreach (var mod in mods)
                om.Write(mod);
            
            om.Write(Globals.VRInstalled);
            
            var dlcs = SRSingleton<GameContext>.Instance.DLCDirector.Installed.ToList();
            om.Write(dlcs.Count);
            foreach (var dlc in dlcs) om.Write((byte)dlc);
            SRMP.Log($"[Join] Sending DLCs: {string.Join(", ", dlcs)}");
            SendMessage(serverUserId, PacketType.Authentication, om);
        }

        public override void OnConnected(ProductUserId remoteUserId, NetworkConnectionType networkType, ConnectionEstablishedType connectionType)
        {
            if (serverUserId == null || remoteUserId != serverUserId)
                return;

            if (Status != NetworkClientStatus.Connecting) return;
            Status = NetworkClientStatus.Authenticating;

            EpicApplication.Instance?.Metrics?.BeginSession();

            SendAuthentication();
        }

        public override void OnDisconnected(ProductUserId remoteUserId, ConnectionClosedReason reason)
        {
            if (serverUserId == null || remoteUserId != serverUserId)
                return;

            SRMP.Log($"Disconnected from server: {reason}");
            SRMP.ReturnToMainMenu($"connection closed ({reason})");
        }
        
        
    }
}
