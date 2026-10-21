using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PuzzleSlotFilled)]
    public class PacketPuzzleSlotFilled : Packet
    {
        public string ID;

        public PacketPuzzleSlotFilled() { }
        public PacketPuzzleSlotFilled(NetIncomingMessage im) { Deserialize(im); }
    }
}
