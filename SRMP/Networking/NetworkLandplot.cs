using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkLandplot : MonoBehaviour
    {
        public NetworkRegion Region;
        public LandPlot Plot;
        public LandPlotLocation Location;

        public bool IsLocal { get { return Region.IsLocal; } }
    }
}
