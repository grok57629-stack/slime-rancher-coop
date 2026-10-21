using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PuzzleSlots)]
    public class PacketPuzzleSlots : Packet
    {
        public struct PuzzleSlotData
        {
            public string ID;
            public PuzzleSlotModel Model;
        }
        public List<PuzzleSlotData> PuzzleSlots { get; set; }

        public PacketPuzzleSlots() { }
        public PacketPuzzleSlots(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(PuzzleSlots.Count);
            foreach(var puzzleSlotData in PuzzleSlots)
            {
                om.Write(puzzleSlotData.ID);
                om.Write(puzzleSlotData.Model.filled);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            PuzzleSlots = new List<PuzzleSlotData>();
            int puzzleSlotCount = im.ReadInt32();
            for(int i = 0; i < puzzleSlotCount; i++)
            {
                PuzzleSlots.Add(new PuzzleSlotData()
                {
                    ID = im.ReadString(),
                    Model = new PuzzleSlotModel()
                    {
                        filled = im.ReadBoolean()
                    }
                });
            }
        }
    }
}
