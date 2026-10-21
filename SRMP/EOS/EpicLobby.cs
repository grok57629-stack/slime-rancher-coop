using Epic.OnlineServices;
using UnityEngine;
using Epic.OnlineServices.Lobby;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRMultiplayer.EpicSDK;
using SRMultiplayer.Networking;

namespace SRMultiplayer.EpicSDK
{
    public class EpicLobby
    {
        private LobbyInterface lobbyInterface;
        private int generation;
        private bool operationPending;

        private ulong? addNotifyLobbyMemberStatusReceivedHandle;
        private ulong? addNotifyLobbyMemberUpdateReceivedHandle;
        private ulong? addNotifyLobbyUpdateReceivedHandle;

        public Utf8String LobbyId { get; private set; }
        public bool IsInLobby => LobbyId != null;
        public bool IsLobbyOwner { get; private set; }

        public NetworkClient NetworkClient { get; private set; }
        public NetworkServer NetworkServer { get; private set; }

        public EpicLobby(LobbyInterface lobbyInterface)
        {
            this.lobbyInterface = lobbyInterface;
        }

        public void Tick()
        {
            NetworkClient?.Tick();
            NetworkServer?.Tick();
        }

        private void RegisterEvents()
        {
            if (!addNotifyLobbyMemberStatusReceivedHandle.HasValue)
            {
                var addNotifyLobbyMemberStatusReceivedOptions = new AddNotifyLobbyMemberStatusReceivedOptions()
                {

                };
                addNotifyLobbyMemberStatusReceivedHandle = lobbyInterface.AddNotifyLobbyMemberStatusReceived(ref addNotifyLobbyMemberStatusReceivedOptions, null, OnLobbyMemberStatusReceived);
            }

            if (!addNotifyLobbyMemberUpdateReceivedHandle.HasValue)
            {
                var notifyLobbyMemberUpdateReceivedOptions = new AddNotifyLobbyMemberUpdateReceivedOptions()
                {

                };
                addNotifyLobbyMemberUpdateReceivedHandle = lobbyInterface.AddNotifyLobbyMemberUpdateReceived(ref notifyLobbyMemberUpdateReceivedOptions, null, OnLobbyMemberUpdateReceived);
            }

            if (!addNotifyLobbyUpdateReceivedHandle.HasValue)
            {
                var addNotifyLobbyUpdateReceivedOptions = new AddNotifyLobbyUpdateReceivedOptions()
                {

                };
                addNotifyLobbyUpdateReceivedHandle = lobbyInterface.AddNotifyLobbyUpdateReceived(ref addNotifyLobbyUpdateReceivedOptions, null, OnLobbyUpdateReceived);
            }

        }

        private void UnregisterEvents()
        {
            if (addNotifyLobbyMemberStatusReceivedHandle.HasValue)
            {
                lobbyInterface.RemoveNotifyLobbyMemberStatusReceived(addNotifyLobbyMemberStatusReceivedHandle.Value);
                addNotifyLobbyMemberStatusReceivedHandle = null;
            }
            if (addNotifyLobbyMemberUpdateReceivedHandle.HasValue)
            {
                lobbyInterface.RemoveNotifyLobbyMemberUpdateReceived(addNotifyLobbyMemberUpdateReceivedHandle.Value);
                addNotifyLobbyMemberUpdateReceivedHandle = null;
            }
            if (addNotifyLobbyUpdateReceivedHandle.HasValue)
            {
                lobbyInterface.RemoveNotifyLobbyUpdateReceived(addNotifyLobbyUpdateReceivedHandle.Value);
                addNotifyLobbyUpdateReceivedHandle = null;
            }
        }

        private void OnLobbyUpdateReceived(ref LobbyUpdateReceivedCallbackInfo data)
        {
            SRMultiplayer.SRMP.LogStack();
        }

        private void OnLobbyMemberUpdateReceived(ref LobbyMemberUpdateReceivedCallbackInfo data)
        {
            SRMultiplayer.SRMP.LogStack();
        }

        private void OnLobbyMemberStatusReceived(ref LobbyMemberStatusReceivedCallbackInfo data)
        {
            if (LobbyId == null || data.LobbyId != LobbyId) return;
            SRMultiplayer.SRMP.Log($"{data.LobbyId} {data.TargetUserId} {data.CurrentStatus}");
            if(data.TargetUserId == SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId)
            {
                if(data.CurrentStatus == LobbyMemberStatus.Closed || data.CurrentStatus == LobbyMemberStatus.Kicked || data.CurrentStatus == LobbyMemberStatus.Disconnected)
                {
                    ForceReset("lobby closed", false);
                }
            }
        }

        public void CreateLobby()
        {
            var authentication = SRSingleton<EpicApplication>.Instance.Authentication;
            if (authentication == null || !authentication.IsLoggedIn || authentication.ProductUserId == null)
            {
                SRMultiplayer.SRMP.Log("[Lobby] Cannot host before EOS login has completed.");
                MultiplayerUI.Instance?.ConnectResponse(MultiplayerUI.ConnectError.Message,
                    "Epic Online Services is still signing in. Wait a moment, then try again.");
                return;
            }
            if (LobbyId != null || operationPending)
            {
                SRMultiplayer.SRMP.Log($"Already in a lobby");
                return;
            }

            var createLobbyOptions = new CreateLobbyOptions()
            {
                AllowedPlatformIds = null,
                AllowInvites = true,
                RejoinAfterKickRequiresInvite = true,
                BucketId = "SRMP",
                CrossplayOptOut = false,
                DisableHostMigration = true,
                EnableJoinById = true,
                LobbyId = GenerateServerCode(),
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId,
                MaxLobbyMembers = (uint)Mathf.Clamp(Globals.MaxPlayers, 2, 64),
                PermissionLevel = LobbyPermissionLevel.Joinviapresence,
                PresenceEnabled = false,
            };
            operationPending = true;
            lobbyInterface.CreateLobby(ref createLobbyOptions, ++generation, OnCreateLobby);
        }

        private void OnCreateLobby(ref CreateLobbyCallbackInfo data)
        {
            if (!(data.ClientData is int token) || token != generation)
            {
                if (data.ResultCode == Result.Success && data.LobbyId != LobbyId)
                    ReleaseRemote(data.LobbyId, true);
                return;
            }
            operationPending = false;
            SRMultiplayer.SRMP.Log($"Result -> {data.ResultCode}");
            if(data.ResultCode != Result.Success)
            {
                return;
            }

            IsLobbyOwner = true;
            LobbyId = data.LobbyId;
            RegisterEvents();

            NetworkServer = SRSingleton<EpicApplication>.Instance.CreateServer();
            NetworkServer.StartListen();
        }

        public void JoinLobby(string lobbyId)
        {
            lobbyId = ServerCode.Normalize(lobbyId);
            if (!ServerCode.IsValid(lobbyId))
            {
                SRMultiplayer.SRMP.Log("[Lobby] Join rejected: server codes must contain exactly 7 letters or digits.");
                MultiplayerUI.Instance?.ConnectResponse(MultiplayerUI.ConnectError.Message, "The server code must contain exactly 7 letters or digits.");
                return;
            }

            var authentication = SRSingleton<EpicApplication>.Instance.Authentication;
            if (authentication == null || !authentication.IsLoggedIn || authentication.ProductUserId == null)
            {
                SRMultiplayer.SRMP.Log("[Lobby] Cannot join before EOS login has completed.");
                MultiplayerUI.Instance?.ConnectResponse(MultiplayerUI.ConnectError.Message,
                    "Epic Online Services is still signing in. Wait a moment, then try again.");
                return;
            }

            if (LobbyId != null || operationPending)
            {
                SRMultiplayer.SRMP.Log($"Already in a lobby");
                return;
            }

            var joinLobbyOptions = new JoinLobbyByIdOptions()
            {
                CrossplayOptOut = false,
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId,
                PresenceEnabled = false,
                LobbyId = lobbyId
            };
            operationPending = true;
            lobbyInterface.JoinLobbyById(ref joinLobbyOptions, ++generation, OnJoinLobby);
        }

        private void OnJoinLobby(ref JoinLobbyByIdCallbackInfo data)
        {
            if (!(data.ClientData is int token) || token != generation)
            {
                if (data.ResultCode == Result.Success && data.LobbyId != LobbyId)
                    ReleaseRemote(data.LobbyId, false);
                return;
            }
            operationPending = false;
            SRMultiplayer.SRMP.Log($"Result -> {data.ResultCode}");
            if(data.ResultCode != Result.Success)
            {
                MultiplayerUI.Instance?.ConnectResponse(MultiplayerUI.ConnectError.Message,
                    "Could not join the server (" + data.ResultCode + "). Check the code and make sure the host is online.");
                return;
            }

            IsLobbyOwner = false;
            LobbyId = data.LobbyId;
            RegisterEvents();

            var lobbyDetails = GetLobbyDetails();
            if (lobbyDetails == null)
            {
                ForceReset("join did not supply lobby details");
                return;
            }
            try
            {
                var options = new LobbyDetailsGetLobbyOwnerOptions();
                var owner = lobbyDetails.GetLobbyOwner(ref options);
                if (owner == null) { ForceReset("join did not supply an owner"); return; }
                var localUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId;
                if (NetworkIdentity.IsSameProductUser(localUserId?.ToString(), owner.ToString()))
                {
                    const string message = "You are already running this server with the same EOS identity. Join from another EOS account/device, or host from this game instead.";
                    SRMP.Log("[Join] Blocked self-join: local and lobby owner Product User IDs match.");
                    MultiplayerUI.Instance?.ConnectResponse(MultiplayerUI.ConnectError.Message, message);
                    ForceReset("self-join uses the host EOS identity", false);
                    return;
                }
                NetworkClient = SRSingleton<EpicApplication>.Instance.CreateClient();
                NetworkClient.Connect(owner);
            }
            finally { lobbyDetails.Release(); }
        }

        public void LeaveLobby()
        {
            if (!IsInLobby || IsLobbyOwner)
                return;
            
            var leaveLobbyOptions = new LeaveLobbyOptions()
            {
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId,
                LobbyId = LobbyId
            };
            lobbyInterface.LeaveLobby(ref leaveLobbyOptions, generation, OnLeaveLobby);
        }

        private void OnLeaveLobby(ref LeaveLobbyCallbackInfo data)
        {
            if (!(data.ClientData is int token) || token != generation || data.LobbyId != LobbyId) return;
            SRMP.Log($"[Lobby] OnLeaveLobby -> {data.ResultCode}");
            ForceReset("OnLeaveLobby completed", false);
        }

        /// <summary>
        /// Drops all local lobby state unconditionally.
        ///
        /// Every other path clears LobbyId only inside a success callback, so a
        /// server that crashed or timed out leaves it set forever - and both
        /// CreateLobby and JoinLobby then refuse with "already in a lobby",
        /// which strands the player: they cannot rejoin and cannot host their
        /// own world until they restart the game.
        /// </summary>
        public void ForceReset(string reason, bool releaseRemote = true)
        {
            if (LobbyId == null && NetworkClient == null && NetworkServer == null && !operationPending) return;
            SRMP.Log($"[Lobby] Reset: {reason}");
            var client = NetworkClient;
            var server = NetworkServer;
            var lobby = LobbyId;
            bool owner = IsLobbyOwner;
            // Shutdown can synchronously load the main menu and re-enter here.
            // Invalidate handles and pending callbacks before doing any of it.
            generation++;
            operationPending = false;
            NetworkClient = null;
            NetworkServer = null;
            LobbyId = null;
            IsLobbyOwner = false;
            try { UnregisterEvents(); } catch (Exception ex) { SRMP.Log($"[Lobby] Unregister failed: {ex}"); }
            try { client?.Shutdown(); } catch (Exception ex) { SRMP.Log($"[Lobby] Client shutdown failed: {ex}"); }
            try { server?.Shutdown(); } catch (Exception ex) { SRMP.Log($"[Lobby] Server shutdown failed: {ex}"); }
            var authentication = SRSingleton<EpicApplication>.Instance?.Authentication;
            if (releaseRemote && lobby != null && authentication != null && authentication.IsLoggedIn
                && authentication.ProductUserId != null)
                ReleaseRemote(lobby, owner);
            else if (releaseRemote && lobby != null)
                SRMultiplayer.SRMP.Log("[Lobby] Local EOS identity is unavailable; skipped the remote leave/destroy request.");
        }

        private void ReleaseRemote(Utf8String lobby, bool owner)
        {
            var user = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId;
            if (owner)
            {
                var options = new DestroyLobbyOptions { LobbyId = lobby, LocalUserId = user };
                lobbyInterface.DestroyLobby(ref options, null, OnReleasedOwner);
            }
            else
            {
                var options = new LeaveLobbyOptions { LobbyId = lobby, LocalUserId = user };
                lobbyInterface.LeaveLobby(ref options, null, OnReleasedMember);
            }
        }

        private static void OnReleasedOwner(ref DestroyLobbyCallbackInfo data)
        {
            SRMP.Log($"[Lobby] Released {data.LobbyId}: {data.ResultCode}");
        }

        private static void OnReleasedMember(ref LeaveLobbyCallbackInfo data)
        {
            SRMP.Log($"[Lobby] Left {data.LobbyId}: {data.ResultCode}");
        }

        public void KickMember(ProductUserId targetUserId)
        {
            var kickMemberOptions = new KickMemberOptions()
            {
                LobbyId = LobbyId,
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId,
                TargetUserId = targetUserId
            };
            lobbyInterface.KickMember(ref kickMemberOptions, null, OnKickMember);
        }

        private void OnKickMember(ref KickMemberCallbackInfo data)
        {
            SRMultiplayer.SRMP.Log($"Result -> {data.ResultCode}");
        }

        public void DestroyLobby()
        {
            if (!IsLobbyOwner || !IsInLobby)
                return;
            
            var destroyLobbyOptions = new DestroyLobbyOptions()
            {
                LobbyId = LobbyId,
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId
            };
            lobbyInterface.DestroyLobby(ref destroyLobbyOptions, generation, OnDestroyLobby);
        }

        private void OnDestroyLobby(ref DestroyLobbyCallbackInfo data)
        {
            if (!(data.ClientData is int token) || token != generation || data.LobbyId != LobbyId) return;
            SRMP.Log($"[Lobby] OnDestroyLobby -> {data.ResultCode}");
            ForceReset("OnDestroyLobby completed", false);
        }

        private LobbyDetails GetLobbyDetails()
        {
            var copyLobbyDetailsHandleOptions = new CopyLobbyDetailsHandleOptions()
            {
                LobbyId = LobbyId,
                LocalUserId = SRSingleton<EpicApplication>.Instance.Authentication.ProductUserId
            };
            var result = lobbyInterface.CopyLobbyDetailsHandle(ref copyLobbyDetailsHandleOptions, out var lobbyDetails);
            if (result != Result.Success)
            {
                SRMultiplayer.SRMP.Log($"CopyLobbyDetailsHandle -> {result}");
            }
            return lobbyDetails;
        }

        public bool ContainsUserId(ProductUserId targetUserId)
        {
            var lobbyDetails = GetLobbyDetails();
            if (lobbyDetails == null) return false;

            var lobbyDetailsGetMemberCountOptions = new LobbyDetailsGetMemberCountOptions() { };
            for (uint i = 0; i < lobbyDetails.GetMemberCount(ref lobbyDetailsGetMemberCountOptions); i++)
            {
                var lobbyDetailsGetMemberByIndexOptions = new LobbyDetailsGetMemberByIndexOptions() { MemberIndex = i };
                var lobbyMember = lobbyDetails.GetMemberByIndex(ref lobbyDetailsGetMemberByIndexOptions);
                if(lobbyMember != null && lobbyMember == targetUserId)
                {
                    lobbyDetails.Release();
                    return true;
                }
            }
            lobbyDetails.Release();
            return false;
        }
        
        public static string GenerateServerCode()
        {
            return ServerCode.Generate();
        }

    }
}
