using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Networking
{
    [Serializable]
    public struct NetworkPlayerSave
    {
        public string LastUsername;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float Rotation;
        public float WeaponY;
        public byte RegionSet;
    }
}
