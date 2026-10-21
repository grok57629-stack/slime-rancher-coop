using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    public abstract class Packet : IPacket
    {
        /// <summary>
        /// Reads a collection size from the wire and rejects values which
        /// cannot be valid for the corresponding game data. This prevents a
        /// corrupt or incompatible peer packet from causing enormous
        /// allocations before the decoder reaches the truncated payload.
        /// </summary>
        protected static int ReadBoundedCount(NetIncomingMessage im, string field, int maximum)
        {
            int count = im.ReadInt32();
            if (count < 0 || count > maximum)
                throw new InvalidDataException("Invalid " + field + " count " + count + " (maximum " + maximum + ").");
            return count;
        }

        /// <summary>
        /// Parameterless constructor to be used through inheritance
        /// </summary>
        public Packet() { }

        /// <summary>
        /// Incoming message based construtor that deserielizes the item for the message
        /// </summary>
        public Packet(NetIncomingMessage im) { Deserialize(im); }


        /// <summary>
        /// Searilizes the given packet item 
        /// </summary>
        /// <param name="om">Outgoing Message that the packet should be added to</param>
        public virtual void Serialize(NetOutgoingMessage om)
        {
            om.Write((ushort)GetPacketType());
            //writes the object type using the gettype name
            //this allows the object to assume its object packet type
            //om.Write(this.GetType().AssemblyQualifiedName);
            //

            om.WriteAllFields(this, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }

        /// <summary>
        /// Deserializes the given packet item 
        /// </summary>
        /// <param name="om">Incoming Message that the packet should be deserialized from</param>
        public virtual void Deserialize(NetIncomingMessage im)
        {
            im.ReadAllFields(this, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        }



        public PacketType GetPacketType()
        {
            PacketAttribute packet = (PacketAttribute)Attribute.GetCustomAttribute(GetType(), typeof(PacketAttribute));
            if (packet != null)
            {
                return packet.Type;
            }
            else
            {
                SRMP.Log($"Packet {GetType()} has no attribute");
                return PacketType.Unknown;
            }
        }
    }
}
