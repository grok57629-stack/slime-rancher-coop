using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkDirectedActorSpawner : MonoBehaviour
    {
        public int ID;
        public DirectedActorSpawner Spawner;
        public NetworkRegion Region;

        public bool IsLocal { get { return Region != null && Region.IsLocal; } }
    }
}
