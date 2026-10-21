using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkHostUI : MonoBehaviour
    {
        public GameObject OfflineMenu;
        public GameObject OnlineMenu;

        private void Awake()
        {
            OfflineMenu = transform.FindDisabled("OfflineMenu").gameObject;
            OnlineMenu = transform.FindDisabled("OnlineMenu").gameObject;
        }

        public void SetOnlineStatus(bool online)
        {
            OfflineMenu.SetActive(!online);
            OnlineMenu.SetActive(online);
        }
    }
}
