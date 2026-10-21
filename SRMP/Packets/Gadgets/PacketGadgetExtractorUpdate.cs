using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetExtractorUpdate)]
    public class PacketGadgetExtractorUpdate : Packet
    {
        public string ID;
        public int cyclesRemaining;
        public int queuedToProduce;
        public double cycleEndTime;
        public double nextProduceTime;

        public PacketGadgetExtractorUpdate() { }
        public PacketGadgetExtractorUpdate(NetIncomingMessage im) { Deserialize(im); }
    }
}
