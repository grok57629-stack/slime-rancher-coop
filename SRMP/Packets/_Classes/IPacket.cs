using Lidgren.Network;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    public interface IPacket
    {
        PacketType GetPacketType();
        /// <summary>
        /// Searilizes the given packet item 
        /// </summary>
        /// <param name="om">Outgoing Message that the packet should be added to</param>
        void Serialize(NetOutgoingMessage om);
        /// <summary>
        /// Deserializes the given packet item 
        /// </summary>
        /// <param name="om">Incoming Message that the packet should be deserialized from</param>
        void Deserialize(NetIncomingMessage im);     
    }
}
