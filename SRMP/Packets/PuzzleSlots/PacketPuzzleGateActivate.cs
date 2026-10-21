using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PuzzleGateActivate)]
    public class PacketPuzzleGateActivate : Packet
    {
        public PacketPuzzleGateActivate() { }
        public PacketPuzzleGateActivate(NetIncomingMessage im) { Deserialize(im); }
    }
}
