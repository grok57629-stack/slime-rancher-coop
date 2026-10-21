using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorFX)]
    public class PacketActorFX : Packet
    {
        public enum FXType
        {
            SlimeEatFX,
            SlimeEatFavoriteFX,
            SlimeTransformFX,
            SlimeProduceFX
        }
        public int ID;
        public byte Type;
        public int Count;

        public PacketActorFX() { }
        public PacketActorFX(NetIncomingMessage im) { Deserialize(im); }
    }
}
