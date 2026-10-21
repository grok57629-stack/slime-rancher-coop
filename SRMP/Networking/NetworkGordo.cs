using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkGordo : MonoBehaviour
    {
        public string ID => Gordo == null ? null : !string.IsNullOrEmpty(Gordo.id) ? Gordo.id : Gordo.gameObject.GetInParent<GadgetSite>()?.id;
        public GordoEat Gordo;
        public NetworkRegion Region;
        internal readonly HashSet<int> PendingFood = new HashSet<int>();
        private bool bursting;

        private void OnDestroy()
        {
            var id = ID;
            if (id != null && Globals.Gordos.TryGetValue(id, out var registered) && registered == this)
                Globals.Gordos.Remove(id);
        }

        public void Eat(Vector3 position, Quaternion rotation)
        {
            if (Gordo.eatFX != null)
            {
                SRBehaviour.SpawnAndPlayFX(Gordo.eatFX, position, rotation);
            }
            if (Gordo.eatCue != null)
            {
                SECTR_AudioSystem.Play(Gordo.eatCue, position, false);
            }
        }

        public void Burst()
        {
            if (bursting || Gordo == null || Gordo.GetEatenCount() == -1) return;
            bursting = true;
            //if object is in active mark the reach target
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ReachedTarget());
            }
            else
            {
                //if not just dismiss the gordo completely
                Gordo.gameObject.SetActive(false);
                Gordo.SetEatenCount(-1);
            }
        }

        //process the gordo burst reaction
        private IEnumerator ReachedTarget()
        {
            //start the burst and begin sounds and animations
            Gordo.WillStartBurst();
            Gordo.GetComponent<GordoFaceAnimator>().SetTrigger("Strain");
            SECTR_AudioSystem.Play(Gordo.strainCue, Gordo.transform.position, false);
            //wait for amination/sounds to finish
            yield return new WaitForSeconds(2f);
            if (Gordo == null) yield break;
            SECTR_AudioSystem.Play(Gordo.burstCue, Gordo.transform.position, false);

            //if the gordo has a destroy effect process it
            if (Gordo.destroyFX != null)
            {
                //play the spawn behavior for destroy events for the gordo that is bursting
                GameObject gameObject = SRBehaviour.SpawnAndPlayFX(Gordo.destroyFX, Gordo.transform.position + Vector3.up * 2f, Gordo.transform.rotation);
                //get the gordo slime type
                Identifiable component = Gordo.gameObject.GetComponent<Identifiable>();
                //get the color of the current gordo
                Color[] colors = SlimeUtil.GetColors(Gordo.gameObject, (component != null) ? component.id : Identifiable.Id.NONE, true);
                //get the slime children spawned by the gordo
                RecolorSlimeMaterial[] componentsInChildren = gameObject.GetComponentsInChildren<RecolorSlimeMaterial>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    //foreach slime in the count spawned by the gordo, set their coloring
                    componentsInChildren[i].SetColors(colors[0], colors[1], colors[2]);
                }
            }
            //trigger the burst completed event
            Gordo.DidCompleteBurst();
            Gordo.rewards?.SetupActiveRewards();

            //despawn the bursted gordo from game
            Gordo.gameObject.SetActive(false);
            Gordo.SetEatenCount(-1);
            yield break;
        }
    }
}
