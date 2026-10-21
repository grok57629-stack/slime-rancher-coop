using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerFX)]
    public class PacketPlayerFX : Packet
    {
        public enum FXType
        {
            VacAudio,
            Vac,
            Airburst,
            CaptureFailed,
            Capture,
            Shoot,
            DestroyOnVac,
            JetpackAudio,
        }

        public byte ID;
        public byte Type;
        public bool Enable;

        public PacketPlayerFX() { }
        public PacketPlayerFX(NetIncomingMessage im) { Deserialize(im); }
    }
}
