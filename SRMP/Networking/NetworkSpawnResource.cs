using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkSpawnResource : MonoBehaviour
    {
        public int ID;
        public SpawnResource SpawnResource;
        public NetworkLandplot LandPlot;
        public NetworkRegion Region;

        public bool IsLocal { get { if (Region == null) return false; return Region.IsLocal; } }

        private void Start()
        {
            if (LandPlot == null)
                LandPlot = GetComponentInParent<NetworkLandplot>();
            if (Region == null)
                Region = GetComponentInParent<NetworkRegion>();
        }

        private void OnDestroy()
        {
            Globals.SpawnResources.Remove(ID);
        }
    }
}
