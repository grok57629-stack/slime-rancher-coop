using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.EpicSDK
{
    public class EpicAuthentication
    {
        private ConnectInterface connectInterface;
        private ulong? connectNotifyLoginStatusChangedId;
        private ulong? connectNotifyAuthExpirationId;
        private bool loginPending;

        public bool DeviceCreated { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public string Username { get; private set; } = "NoUsername";
        public ProductUserId ProductUserId { get; private set; }

        public EpicAuthentication(ConnectInterface connectInterface) 
        { 
            this.connectInterface = connectInterface;

            DeviceCreated = false;

            var createDeviceIdOptions = new CreateDeviceIdOptions()
            {
                DeviceModel = "PC Windows"
            };
            connectInterface.CreateDeviceId(ref createDeviceIdOptions, null, OnCreateDeviceId);
        }

        public void Login(string username)
        {
            if (!DeviceCreated)
            {
                SRMultiplayer.SRMP.Log($"Cannot Login while no device was created");
                return;
            }
            if (IsLoggedIn)
            {
                SRMultiplayer.SRMP.Log($"Already logged in");
                return;
            }
            if (loginPending)
            {
                SRMultiplayer.SRMP.Log("EOS login is already in progress; ignoring a duplicate request.");
                return;
            }

            Username = username;
            loginPending = true;

            var connectLoginOptions = new Epic.OnlineServices.Connect.LoginOptions()
            {
                UserLoginInfo = new Epic.OnlineServices.Connect.UserLoginInfo()
                {
                    DisplayName = Username
                },
                Credentials = new Epic.OnlineServices.Connect.Credentials()
                {
                    Type = ExternalCredentialType.DeviceidAccessToken,
                    Token = null
                }
            };

            connectInterface.Login(ref connectLoginOptions, this, OnConnectLogin);
        }

        public void Logout()
        {
            if(!IsLoggedIn)
            {
                SRMultiplayer.SRMP.Log($"Not logged in");
                return;
            }

            var logoutOptions = new LogoutOptions()
            {
                LocalUserId = ProductUserId
            };
            connectInterface.Logout(ref logoutOptions, null, OnLogout);
        }

        internal void ReauthenticateAfterInvalidP2PIdentity()
        {
            if (!DeviceCreated)
            {
                SRMultiplayer.SRMP.Log("[EOS] Cannot reauthenticate because the local Device ID is not ready.");
                return;
            }
            SRMultiplayer.SRMP.Log("[EOS] Local P2P identity was rejected; clearing it and requesting a fresh Connect login.");
            IsLoggedIn = false;
            ProductUserId = null;
            if (loginPending) return;
            loginPending = false;
            Login(Username);
        }

        private void OnLogout(ref LogoutCallbackInfo data)
        {
            if (data.ResultCode == Result.Success)
            {
                IsLoggedIn = false;
                ProductUserId = null;
            }
            else if (Common.IsOperationComplete(data.ResultCode))
            {
                SRMultiplayer.SRMP.Log($"Result -> {data.ResultCode}");
            }
        }

        private void OnCreateDeviceId(ref CreateDeviceIdCallbackInfo data)
        {
            if (data.ResultCode == Result.Success || data.ResultCode == Result.DuplicateNotAllowed)
            {
                DeviceCreated = true;
                
                Login(Globals.Username);
            }
            else if (Common.IsOperationComplete(data.ResultCode))
            {
                SRMultiplayer.SRMP.Log($"Result -> {data.ResultCode}");
            }
        }

        private void OnConnectLogin(ref Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo)
        {
            if (!ReferenceEquals(loginCallbackInfo.ClientData, this)) return;
            SRMultiplayer.SRMP.Log($"{loginCallbackInfo.ResultCode} {loginCallbackInfo.LocalUserId}");

            if (loginCallbackInfo.ResultCode == Result.Success)
            {
                loginPending = false;
                ProductUserId = loginCallbackInfo.LocalUserId;

                if (!connectNotifyLoginStatusChangedId.HasValue)
                {
                    var addNotifyLoginStatusChangedOptions = new Epic.OnlineServices.Connect.AddNotifyLoginStatusChangedOptions();
                    var notifyId = connectInterface.AddNotifyLoginStatusChanged(ref addNotifyLoginStatusChangedOptions, null, OnConnectLoginStatusChanged);
                    if (notifyId != Common.InvalidNotificationid)
                    {
                        connectNotifyLoginStatusChangedId = notifyId;
                    }
                }

                if (!connectNotifyAuthExpirationId.HasValue)
                {
                    var addNotifyAuthExpirationOptions = new Epic.OnlineServices.Connect.AddNotifyAuthExpirationOptions();
                    var notifyId = connectInterface.AddNotifyAuthExpiration(ref addNotifyAuthExpirationOptions, null, OnConnectAuthExpiration);
                    if (notifyId != Common.InvalidNotificationid)
                    {
                        connectNotifyAuthExpirationId = notifyId;
                    }
                }

                IsLoggedIn = true;
            }
            else if (loginCallbackInfo.ResultCode == Result.InvalidUser)
            {
                var createUserOptions = new Epic.OnlineServices.Connect.CreateUserOptions()
                {
                    ContinuanceToken = loginCallbackInfo.ContinuanceToken
                };

                connectInterface.CreateUser(ref createUserOptions, this, OnCreateUser);
            }
            else if (Common.IsOperationComplete(loginCallbackInfo.ResultCode))
            {
                loginPending = false;
                SRMultiplayer.SRMP.Log($"Result -> {loginCallbackInfo.ResultCode}");
            }
        }

        private void OnConnectAuthExpiration(ref AuthExpirationCallbackInfo authExpirationCallbackInfo)
        {
            if (ProductUserId == null || authExpirationCallbackInfo.LocalUserId != ProductUserId) return;
            IsLoggedIn = false;

            Login(Username);
        }

        private void OnConnectLoginStatusChanged(ref Epic.OnlineServices.Connect.LoginStatusChangedCallbackInfo loginStatusChangedCallbackInfo)
        {
            if (ProductUserId == loginStatusChangedCallbackInfo.LocalUserId)
            {
                if (loginStatusChangedCallbackInfo.PreviousStatus == LoginStatus.LoggedIn && loginStatusChangedCallbackInfo.CurrentStatus == LoginStatus.NotLoggedIn)
                {
                    IsLoggedIn = false;

                    if (connectNotifyLoginStatusChangedId.HasValue)
                    {
                        connectInterface.RemoveNotifyLoginStatusChanged(connectNotifyLoginStatusChangedId.Value);
                        connectNotifyLoginStatusChangedId = null;
                    }

                    if (connectNotifyAuthExpirationId.HasValue)
                    {
                        connectInterface.RemoveNotifyAuthExpiration(connectNotifyAuthExpirationId.Value);
                        connectNotifyAuthExpirationId = null;
                    }

                    ProductUserId = null;
                }
            }
        }

        private void OnCreateUser(ref CreateUserCallbackInfo createUserCallbackInfo)
        {
            if (!ReferenceEquals(createUserCallbackInfo.ClientData, this)) return;
            loginPending = false;
            SRMultiplayer.SRMP.Log($"{createUserCallbackInfo.ResultCode} {createUserCallbackInfo.LocalUserId}");

            if (createUserCallbackInfo.ResultCode == Result.Success)
            {
                Login(Username);
            }
            else if (Common.IsOperationComplete(createUserCallbackInfo.ResultCode))
            {
                SRMultiplayer.SRMP.Log($"Result -> {createUserCallbackInfo.ResultCode}");
            }
        }
    }
}
