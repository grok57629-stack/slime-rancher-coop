using UnityEngine;
using System.Collections;
using System;

namespace SRMultiplayer.Networking
{
    public class NetworkNutcracker : MonoBehaviour
    {
        public int ID;
        public Nutcracker Cracker;

        internal void Crack(GameObject toCrack)
        {
            StartCoroutine(DoCrack(toCrack));
        }

        internal IEnumerator DoCrack(GameObject toCrack)
        {
            if (toCrack == null || Cracker == null) yield break;
            Vector3 position = toCrack.transform.position;
            Quaternion rotation = toCrack.transform.rotation;
            toCrack.GetComponent<Rigidbody>().isKinematic = true;
            toCrack.GetComponent<Collider>().enabled = false;
            Renderer[] componentsInChildren = toCrack.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = false;
            }
            Cracker.anim.SetTrigger(Cracker.activateId);
            if (Cracker.crackCue != null)
            {
                SECTR_AudioSystem.Play(Cracker.crackCue, Cracker.transform.position, false);
            }
            yield return new WaitForSeconds(3f);
            if (Cracker != null && Cracker.crackFX != null)
            {
                SRBehaviour.SpawnAndPlayFX(Cracker.crackFX, position, rotation);
            }
        }
    }
}
