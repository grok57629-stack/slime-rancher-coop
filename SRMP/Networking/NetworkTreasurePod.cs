using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkTreasurePod : MonoBehaviour
    {
        public TreasurePod Pod;
        public NetworkRegion Region;

        public void Activate()
        {
            Pod.CurrState = TreasurePod.State.OPEN;
            if (gameObject.activeInHierarchy)
            {
                if (Pod.openCue != null)
                {
                    SECTR_AudioSystem.Play(Pod.openCue, Pod.transform.position, false);
                }
                if (Pod.openFX != null)
                {
                    SRBehaviour.InstantiateDynamic(Pod.openFX, Pod.transform.position, Pod.transform.rotation, false);
                }
                StartCoroutine(AwardPrizesDefault());
            }
            else
            {
                if (Pod.unlockedSlimeAppearance != null)
                {
                    Pod.slimeAppearanceDirector.UnlockAppearance(Pod.unlockedSlimeAppearanceDefinition, Pod.unlockedSlimeAppearance);
                }
                if (Pod.blueprint != Gadget.Id.NONE)
                {
                    SRSingleton<SceneContext>.Instance.PopupDirector.QueueForPopup(new TreasurePod.BlueprintPopupCreator(Pod.blueprint));
                    SRSingleton<SceneContext>.Instance.PopupDirector.MaybePopupNext();
                }
                if (Pod.unlockedSlimeAppearance != null)
                {
                    Pod.slimeAppearanceDirector.UpdateChosenSlimeAppearance(Pod.unlockedSlimeAppearanceDefinition, Pod.unlockedSlimeAppearance);
                    Pod.unlockedSlimeAppearance.MaybeShowPopupUI();
                }
            }
        }

        private IEnumerator AwardPrizesDefault()
        {
            if (Pod.unlockedSlimeAppearance != null)
            {
                Pod.slimeAppearanceDirector.UnlockAppearance(Pod.unlockedSlimeAppearanceDefinition, Pod.unlockedSlimeAppearance);
            }
            yield return new WaitForSeconds(TreasurePod.OPEN_DELAY);
            if (Pod.afterOpenFX != null)
            {
                SRBehaviour.InstantiateDynamic(Pod.afterOpenFX, base.transform.position, base.transform.rotation, false);
            }
            if (Pod.blueprint != Gadget.Id.NONE)
            {
                SRSingleton<SceneContext>.Instance.PopupDirector.QueueForPopup(new TreasurePod.BlueprintPopupCreator(Pod.blueprint));
                SRSingleton<SceneContext>.Instance.PopupDirector.MaybePopupNext();
                yield return new WaitForSeconds(TreasurePod.ITEM_GAP_DELAY);
            }
            if (Pod.unlockedSlimeAppearance != null)
            {
                Pod.slimeAppearanceDirector.UpdateChosenSlimeAppearance(Pod.unlockedSlimeAppearanceDefinition, Pod.unlockedSlimeAppearance);
                Pod.unlockedSlimeAppearance.MaybeShowPopupUI();
            }
        }
    }
}
