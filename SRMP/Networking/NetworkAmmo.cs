using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkAmmo : Ammo
    {
        public static Dictionary<string, NetworkAmmo> All = new Dictionary<string, NetworkAmmo>();

        public string ID;
        public SiloStorage Silo;

        public NetworkAmmo(SiloStorage silo, HashSet<Identifiable.Id> potentialAmmo, int numSlots, int usableSlots, Predicate<Identifiable.Id>[] slotPreds, Func<Identifiable.Id, int, int> slotMaxCountFunction) : base(potentialAmmo, numSlots, usableSlots, slotPreds, slotMaxCountFunction)
        {
            Silo = silo;
            var landplotLocation = GetInParent<LandPlotLocation>(silo.gameObject);
            if (landplotLocation != null)
            {
                ID = landplotLocation.id;
            }
            else
            {
                ID = GetInParent<GadgetSite>(silo.gameObject).id;
            }
            ID += "-" + silo.name;

            All[ID] = this;
        }

        private T GetInParent<T>(GameObject obj)
        {
            var cmp = obj.GetComponent<T>();
            if(cmp != null)
            {
                return cmp;
            }
            if(obj.transform.parent != null)
            {
                return GetInParent<T>(obj.transform.parent.gameObject);
            }
            return default(T);
        }
    }
}
