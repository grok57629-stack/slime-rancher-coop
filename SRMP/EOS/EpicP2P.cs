using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Packets;
using SRMultiplayer.Networking;
using UnityEngine;

namespace SRMultiplayer.EpicSDK
{
    public abstract class EpicP2P
    {
        private readonly Dictionary<ProductUserId, PacketReassembler> reassemblers = new Dictionary<ProductUserId, PacketReassembler>();
        private uint nextMessageId;
        private bool active;
        private bool localIdentityFailureQueued;
        private const int MaxPacketsPerTick = 512;
        private const float ReceiveBudgetSeconds = 0.003f;
        
        private static SocketId SRMP_SOCKETID = new SocketId { SocketName = "SRMP" };

        private P2PInterface p2PInterface;
        private bool isServer;

        private ulong? notifyPeerConnectionRequestHandle;
        private ulong? notifyIncomingPacketQueueFullHandle;
        private ulong? notifyPeerConnectionClosedHandle;
        private ulong? notifyPeerConnectionEstablishedHandle;
        private ulong? notifyPeerConnectionInterruptedHandle;

        private Queue<Action> queuedActions = new Queue<Action>();

        public EpicP2P(P2PInterface p2PInterface, bool isServer)
        {
            this.p2PInterface = p2PInterface;
            this.isServer = isServer;
        }

        public void Tick()
        {
            if (!active) return;
            var authentication = EpicApplication.Instance?.Authentication;
            if (authentication == null || !authentication.IsLoggedIn || authentication.ProductUserId == null)
                QueueLocalIdentityFailure("EOS authentication is no longer available to this P2P session");
            // EOS callbacks establish player identities before their first
            // authentication/data packet is drained in this same frame.
            var actionCount = queuedActions.Count;
            for (int i = 0; i < actionCount && queuedActions.Count > 0 && active; i++)
            {
                var action = queuedActions.Dequeue();
                try { action(); }
                catch (Exception ex) { SRMP.Log($"[Network] Connection callback failed: {ex}"); }
            }
            if (!active) return;
            Receive();
            foreach (var receiver in reassemblers.Values) receiver.Expire(Time.realtimeSinceStartup);
            if (active) OnTick();
        }

        protected void SetRelayControl(RelayControl relayControl)
        {
            var setRelayControlOptions = new SetRelayControlOptions
            {
                RelayControl = relayControl
            };
            p2PInterface.SetRelayControl(ref setRelayControlOptions);
        }

        protected void SetupP2P()
        {
            if (active) return;
            active = true;
            var queueOptions = new SetPacketQueueSizeOptions
            {
                IncomingPacketQueueMaxSizeBytes = 16 * 1024 * 1024,
                OutgoingPacketQueueMaxSizeBytes = 16 * 1024 * 1024,
            };
            p2PInterface.SetPacketQueueSize(ref queueOptions);
            var addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                SocketId = SRMP_SOCKETID
            };
            notifyPeerConnectionRequestHandle = p2PInterface.AddNotifyPeerConnectionRequest(ref addNotifyPeerConnectionRequestOptions, null, OnPeerConnectionRequest);

            var addNotifyIncomingPacketQueueFullOptions = new AddNotifyIncomingPacketQueueFullOptions();
            
            notifyIncomingPacketQueueFullHandle = p2PInterface.AddNotifyIncomingPacketQueueFull(ref addNotifyIncomingPacketQueueFullOptions, null, OnIncomingPacketQueueFull);

            var addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                SocketId = SRMP_SOCKETID
            };
            notifyPeerConnectionClosedHandle = p2PInterface.AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions, null, OnPeerConnectionClosed);

            var addNotifyPeerConnectionEstablishedOptions = new AddNotifyPeerConnectionEstablishedOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                SocketId = SRMP_SOCKETID
            };
            notifyPeerConnectionEstablishedHandle = p2PInterface.AddNotifyPeerConnectionEstablished(ref addNotifyPeerConnectionEstablishedOptions, null, OnPeerConnectionEstablished);

            var addNotifyPeerConnectionInterruptedOptions = new AddNotifyPeerConnectionInterruptedOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                SocketId = SRMP_SOCKETID
            };
            notifyPeerConnectionInterruptedHandle = p2PInterface.AddNotifyPeerConnectionInterrupted(ref addNotifyPeerConnectionInterruptedOptions, null, OnPeerConnectionInterrupted);
            
        }

        public void Shutdown()
        {
            if (!active) return;
            active = false;
            queuedActions.Clear();
            reassemblers.Clear();
            if (notifyPeerConnectionRequestHandle.HasValue)
            {
                p2PInterface.RemoveNotifyPeerConnectionRequest(notifyPeerConnectionRequestHandle.Value);
                notifyPeerConnectionRequestHandle = null;
            }
            if (notifyIncomingPacketQueueFullHandle.HasValue)
            {
                p2PInterface.RemoveNotifyIncomingPacketQueueFull(notifyIncomingPacketQueueFullHandle.Value);
                notifyIncomingPacketQueueFullHandle = null;
            }
            if (notifyPeerConnectionClosedHandle.HasValue)
            {
                p2PInterface.RemoveNotifyPeerConnectionClosed(notifyPeerConnectionClosedHandle.Value);
                notifyPeerConnectionClosedHandle = null;
            }
            if (notifyPeerConnectionEstablishedHandle.HasValue)
            {
                p2PInterface.RemoveNotifyPeerConnectionEstablished(notifyPeerConnectionEstablishedHandle.Value);
                notifyPeerConnectionEstablishedHandle = null;
            }
            if (notifyPeerConnectionInterruptedHandle.HasValue)
            {
                p2PInterface.RemoveNotifyPeerConnectionInterrupted(notifyPeerConnectionInterruptedHandle.Value);
                notifyPeerConnectionInterruptedHandle = null;
            }

            OnShutdown();
        }

        protected void ForgetPeer(ProductUserId peer)
        {
            if (peer != null) reassemblers.Remove(peer);
        }

        protected void SendMessage(ProductUserId peer, PacketType type, NetOutgoingMessage message,
            PacketReliability reliability = PacketReliability.ReliableOrdered)
        {
            if (!active || localIdentityFailureQueued || peer == null) return;
            // Fragmented state must be reliable: losing one piece otherwise
            // loses the entire snapshot. LengthBytes excludes buffer capacity.
            if (message.LengthBytes > PacketFraming.FragmentSize)
                reliability = PacketReliability.ReliableOrdered;
            foreach (var frame in PacketFraming.Encode((ushort)type, ++nextMessageId, message.Data, message.LengthBytes))
                SendDataInternal(peer, frame, reliability);
        }

        protected bool TryReadMessage(ProductUserId peer, ref NetIncomingMessage message, out PacketType type)
        {
            if (!reassemblers.TryGetValue(peer, out var receiver))
            {
                receiver = new PacketReassembler();
                reassemblers.Add(peer, receiver);
            }
            bool complete = receiver.TryReceive(message.Data, message.LengthBytes, Time.realtimeSinceStartup,
                out ushort packetType, out byte[] payload);
            type = (PacketType)packetType;
            if (!complete) return false;
            message = new NetIncomingMessage { Data = payload, LengthBytes = payload.Length };
            if (type != PacketType.Authentication && message.ReadUInt16() != packetType)
                throw new System.IO.InvalidDataException("SRMP payload type does not match its frame.");
            return true;
        }

        protected virtual void OnTick() { }

        public virtual void OnShutdown() { }

        protected virtual void OnLocalIdentityInvalidated(string reason)
        {
            SRMP.Log("[EOS] " + reason);
        }

        private void QueueLocalIdentityFailure(string reason)
        {
            if (localIdentityFailureQueued) return;
            localIdentityFailureQueued = true;
            SRMP.Log("[EOS] " + reason + "; stopping this session instead of repeating invalid-user P2P calls every frame.");
            EpicApplication.Instance?.Authentication?.ReauthenticateAfterInvalidP2PIdentity();
            queuedActions.Enqueue(() => OnLocalIdentityInvalidated(reason));
        }

        private void Receive()
        {
            if (localIdentityFailureQueued) return;
            float receiveStarted = Time.realtimeSinceStartup;
            int receivedPackets = 0;
            var getNextReceivedPacketSizeOptions = new GetNextReceivedPacketSizeOptions()
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                RequestedChannel = 0
            };
            while (active && receivedPackets < MaxPacketsPerTick)
            {
                if (receivedPackets > 0 && Time.realtimeSinceStartup - receiveStarted >= ReceiveBudgetSeconds)
                    break;

                var result = p2PInterface.GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out uint outPacketSizeBytes);

                if (result == Result.InvalidUser)
                {
                    QueueLocalIdentityFailure("EOS rejected the local Product User ID while reading P2P data");
                    break;
                }
                if(result != Result.Success || outPacketSizeBytes == 0)
                {
                    break;
                }

                var receivePacketOptions = new ReceivePacketOptions()
                {
                    LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                    RequestedChannel = 0,
                    MaxDataSizeBytes = outPacketSizeBytes
                };

                ProductUserId senderUserId = null;
                SocketId socketId = SocketId.Empty;
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[outPacketSizeBytes]);
                result = p2PInterface.ReceivePacket(ref receivePacketOptions, ref senderUserId, ref socketId, out byte outChannel, buffer, out uint outBytesWritten);
                if (result == Result.InvalidUser)
                {
                    QueueLocalIdentityFailure("EOS rejected the local Product User ID while receiving P2P data");
                    break;
                }
                if (result != Result.Success) break;
                receivedPackets++;

                if(senderUserId != null && outBytesWritten > 0 && socketId.SocketName == SRMP_SOCKETID.SocketName)
                {
                    NetIncomingMessage incomingMessage = new NetIncomingMessage();
                    incomingMessage.LengthBytes = (int)outBytesWritten;
                    incomingMessage.Data = buffer.Array;
                    try { OnMessageReceived(senderUserId, outChannel, ref incomingMessage); }
                    catch (Exception ex)
                    {
                        SRMP.Log($"[Network] Packet from {senderUserId} failed ({outBytesWritten} bytes): {ex}");
                        if (ex is System.IO.InvalidDataException)
                        {
                            SRMP.Log($"[Network] Closing peer {senderUserId} after invalid or incompatible packet data.");
                            CloseConnection(senderUserId);
                        }
                    }
                }   
            }
        }

        public virtual void OnMessageReceived(ProductUserId senderUserId, byte channel, ref NetIncomingMessage im)
        {
            
        }

        protected void SendDataInternal(ProductUserId receiverUserId, byte[] data, PacketReliability packetReliability = PacketReliability.ReliableOrdered)//, byte channel = 0)
        {
            if (localIdentityFailureQueued || EpicApplication.Instance?.Authentication?.ProductUserId == null)
                return;
            var sendPacketOptions = new SendPacketOptions()
            {
                AllowDelayedDelivery = false,
                DisableAutoAcceptConnection = true,
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                Reliability = packetReliability,
                Channel = 0,
                RemoteUserId = receiverUserId,
                SocketId = SRMP_SOCKETID,
                Data = new ArraySegment<byte>(data)
            };
            var result = p2PInterface.SendPacket(ref sendPacketOptions);
            if(result != Result.Success)
            {
                SRMP.Log($"Failed to send packet: Error - {result}");
            }
        }

        private void OnPeerConnectionInterrupted(ref OnPeerConnectionInterruptedInfo data)
        {
            var queuedData = data;
            queuedActions.Enqueue(() =>
            {
                OnConnectionInterrupted(queuedData.RemoteUserId);
            });
        }

        public virtual void OnConnectionInterrupted(ProductUserId remoteUserId)
        {
            
        }

        private void OnPeerConnectionEstablished(ref OnPeerConnectionEstablishedInfo data)
        {
            SRMP.Log("Peer connection established!");
            var queuedData = data;
            queuedActions.Enqueue(() =>
            {
                OnConnected(queuedData.RemoteUserId, queuedData.NetworkType, queuedData.ConnectionType);
            });
        }

        public virtual void OnConnected(ProductUserId remoteUserId, NetworkConnectionType networkType, ConnectionEstablishedType connectionType) { }

        private void OnPeerConnectionClosed(ref OnRemoteConnectionClosedInfo data)
        {
            var queuedData = data;
            queuedActions.Enqueue(() =>
            {
                OnDisconnected(queuedData.RemoteUserId, queuedData.Reason);
            });
        }

        public virtual void OnDisconnected(ProductUserId remoteUserId, ConnectionClosedReason reason) { }

        private void OnIncomingPacketQueueFull(ref OnIncomingPacketQueueFullInfo data)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{data.OverflowPacketChannel} {data.OverflowPacketSizeBytes} {data.PacketQueueCurrentSizeBytes} {data.PacketQueueMaxSizeBytes}");
        }

        private void OnPeerConnectionRequest(ref OnIncomingConnectionRequestInfo data)
        {
            if (isServer)
            {
                var queuedData = data;
                queuedActions.Enqueue(new Action(() =>
                {
                    OnConnectionRequest(queuedData.RemoteUserId);
                }));
            }
            else
            {
                CloseConnection(data.RemoteUserId);
            }
        }

        protected void AcceptConnection(ProductUserId remoteUserId)
        {
            if (localIdentityFailureQueued || remoteUserId == null
                || EpicApplication.Instance?.Authentication?.ProductUserId == null)
                return;
            var acceptConnectionOptions = new AcceptConnectionOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                RemoteUserId = remoteUserId,
                SocketId = SRMP_SOCKETID
            };
            p2PInterface.AcceptConnection(ref acceptConnectionOptions);
        }

        protected void CloseConnection(ProductUserId remoteUserId)
        {
            if (localIdentityFailureQueued || remoteUserId == null
                || EpicApplication.Instance?.Authentication?.ProductUserId == null)
                return;
            var closeConnectionOptions = new CloseConnectionOptions
            {
                LocalUserId = EpicApplication.Instance.Authentication.ProductUserId,
                RemoteUserId = remoteUserId,
                SocketId = SRMP_SOCKETID
            };
            p2PInterface.CloseConnection(ref closeConnectionOptions);
        }

        public virtual void OnConnectionRequest(ProductUserId remoteUserId)
        {
            
        }
    }
}
