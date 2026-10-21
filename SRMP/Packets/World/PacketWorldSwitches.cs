using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldSwitches)]
    public class PacketWorldSwitches : Packet
    {
        public struct WorldSwitchData
        {
            public string ID;
            public MasterSwitchModel Model;
        }
        public List<WorldSwitchData> Switches { get; set; }

        public PacketWorldSwitches() { }
        public PacketWorldSwitches(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Switches.Count);
            foreach(var switchData in Switches)
            {
                om.Write(switchData.ID);
                om.Write((byte)switchData.Model.state);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Switches = new List<WorldSwitchData>();
            int switchCount = im.ReadInt32();
            for(int i = 0; i < switchCount; i++)
            {
                Switches.Add(new WorldSwitchData()
                {
                    ID = im.ReadString(),
                    Model = new MasterSwitchModel()
                    {
                        state = (SwitchHandler.State)im.ReadByte()
                    }
                });
            }
        }
    }
}
