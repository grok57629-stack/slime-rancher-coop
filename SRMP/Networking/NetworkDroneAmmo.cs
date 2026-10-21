using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Networking
{
    public class NetworkDroneAmmo : DroneAmmo
    {
        public Drone Drone;

        public NetworkDroneAmmo(Drone drone) : base()
        {
            Drone = drone;
        }
    }
}
