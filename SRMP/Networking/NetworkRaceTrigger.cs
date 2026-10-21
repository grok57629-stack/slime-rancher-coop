using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkRaceTrigger : MonoBehaviour
    {
        public int ID;

        public QuicksilverAmmoReplacer Ammo;
        public QuicksilverEnergyCheckpoint Checkpoint;
        public QuicksilverEnergyReplacer Energy;

        public void Activate()
        {
            if(Ammo != null)
            {
                SECTR_AudioSystem.Play(Ammo.picked.onPickupCue, Ammo.transform.position, false);
                Ammo.unavailUntil = Ammo.timeDir.HoursFromNow(Ammo.cooldownHours);
                Ammo.picked = Ammo.PickNextAmmo();
            }
            if(Checkpoint != null)
            {
                SECTR_AudioSystem.Play(Checkpoint.onPickupCue, Checkpoint.transform.position, false);
                Checkpoint.cooldown = Checkpoint.timeDirector.HoursFromNow(Checkpoint.cooldownHours);
                Checkpoint.onPickupFX.SetActive(true);
            }
            if(Energy != null)
            {
                SECTR_AudioSystem.Play(Energy.onPickupCue, Energy.transform.position, false);
                Energy.activeTime = Energy.timeDirector.HoursFromNow(Energy.cooldownHours);
            }
        }
    }
}
