using Lidgren.Network;

namespace SRMultiplayer.Packets.ModCompat
{
    [Packet(PacketType.SpiritSlimeMode)]
    public class PacketSpiritMode : Packet
    {
        public enum BehaviorMode
        {
            Drain = -1, None = 0, Heal = 1,
        }
        
        public BehaviorMode Mode;
        
        public PacketSpiritMode() {}
        public PacketSpiritMode(NetIncomingMessage im) { Deserialize(im); }

        public override void Deserialize(NetIncomingMessage im)
        {
            Mode = (BehaviorMode) im.ReadSByte();
        }

        public override void Serialize(NetOutgoingMessage om)
        {
            om.Write((ushort)GetPacketType());
            
            om.Write((sbyte)Mode);
        }
    }
}
