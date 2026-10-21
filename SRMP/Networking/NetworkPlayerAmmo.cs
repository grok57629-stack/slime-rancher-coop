using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Networking
{
    public class NetworkPlayerAmmo : Ammo
    {
        public byte ID;

        public NetworkPlayerAmmo(byte id, HashSet<Identifiable.Id> potentialAmmo, int numSlots, int usableSlots, Predicate<Identifiable.Id>[] slotPreds, Func<Identifiable.Id, int, int> slotMaxCountFunction) : base(potentialAmmo, numSlots, usableSlots, slotPreds, slotMaxCountFunction)
        {
            ID = id;
        }
    }
}
