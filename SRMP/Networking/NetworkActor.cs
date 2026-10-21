using DG.Tweening;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Lidgren.Network;
#if SRML
using SRML;
#endif
using SRMultiplayer.Networking.ModCompat;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkActor : MonoBehaviour
    {
        public int ID { get; internal set; }
        public byte Owner;
        public ushort Ident;
        public byte RegionSet;
        public bool IsLocal { get { return Globals.IsMultiplayer && Owner != 0 && Owner == Globals.LocalID; } }
        public List<NetworkPlayer> KnownPlayers = new List<NetworkPlayer>();

        private Rigidbody m_Rigidbody;
        private RegionMember m_RegionMember;
        private float m_MovementUpdateTimer;
        private float m_LastPositionSent;
        private bool m_OwnershipRequested;
        private Vector3 m_ActualPosition;
        private Vector3 m_LatestPosition;
        private Vector3 m_PreviousPosition;
        private Quaternion m_ActualRotation;
        private Quaternion m_LatestRotation;
        private Quaternion m_PreviousRotation;
        private float m_LatestPositionTime;
        private float m_InterpolationPeriod = 0.1f;
        private Dictionary<SlimeEmotions.Emotion, float> m_PreviousEmotions = new Dictionary<SlimeEmotions.Emotion, float>();

        public Reproduce Reproduce;
        public SlimeEat SlimeEat;
        public ResourceCycle ResourceCycle;
        public DestroyPlortAfterTime DestroyPlortAfterTime;
        private static readonly HashSet<int> reportedDestroyedNetworkActors = new HashSet<int>();

        /// <summary>
        /// Unity keeps destroyed components comparable as managed references,
        /// so a dictionary TryGetValue alone does not prove its native object
        /// still exists. Packet handlers must drop those stale ids instead of
        /// throwing from GetComponent/transform and abandoning an entire batch.
        /// </summary>
        internal static bool TryGetRegistered(int id, out NetworkActor actor)
        {
            if (Globals.Actors.TryGetValue(id, out actor) && actor != null && actor.gameObject != null)
                return true;

            if (Globals.Actors.TryGetValue(id, out var registered) && registered == null)
            {
                Globals.Actors.Remove(id);
                if (reportedDestroyedNetworkActors.Add(id))
                    SRMP.Log($"[Network] Dropped a packet for destroyed actor {id} and removed its stale registry entry.");
            }
            actor = null;
            return false;
        }

        /// <summary>
        /// Retire a replicated actor even when one of its optional destruction
        /// effects has already lost its Unity object. Visual effects must never
        /// prevent the authoritative destroy from reaching the rest of the world.
        /// </summary>
        internal static void DestroyRegistered(int id, string reason)
        {
            if (!TryGetRegistered(id, out NetworkActor actor)) return;

            try
            {
                GameObject actorObject = actor.gameObject;
                if (actorObject == null) return;

                try
                {
                    actor.OnDestroyEffect();
                }
                catch (Exception ex)
                {
                    NetworkDiagnostics.LogPacketFailure(
                        $"[Network] Actor {id} destroy effect failed; continuing despawn", ex);
                }

                try
                {
                    Destroyer.DestroyActor(actorObject, reason);
                }
                catch (Exception ex)
                {
                    NetworkDiagnostics.LogPacketFailure(
                        $"[Network] Actor {id} game cleanup failed; falling back to Unity destroy", ex);
                    try
                    {
                        if (actorObject != null) UnityEngine.Object.Destroy(actorObject);
                    }
                    catch (Exception fallbackException)
                    {
                        NetworkDiagnostics.LogPacketFailure(
                            $"[Network] Actor {id} fallback destroy failed", fallbackException);
                    }
                }
            }
            finally
            {
                // Remove stale IDs even if the object was already destroyed or
                // the game's cleanup/effect code throws. Repeated destroy packets
                // then become harmless no-ops.
                Globals.Actors.Remove(id);
            }
        }

#if SRML
        // Mod Compatibility
        public NetworkSpiritSlime SpiritModdedBehavior;
#endif
        
        private void Awake()
        {
            m_Rigidbody = GetComponentInChildren<Rigidbody>();
            m_RegionMember = GetComponent<RegionMember>();
            Reproduce = GetComponent<Reproduce>();
            SlimeEat = GetComponent<SlimeEat>();
            ResourceCycle = GetComponent<ResourceCycle>();
            DestroyPlortAfterTime = GetComponent<DestroyPlortAfterTime>();
            PositionRotationUpdate(transform.position, transform.rotation, true);

            m_PreviousEmotions.Add(SlimeEmotions.Emotion.AGITATION, 0);
            m_PreviousEmotions.Add(SlimeEmotions.Emotion.FEAR, 0);
            m_PreviousEmotions.Add(SlimeEmotions.Emotion.HUNGER, 0);

            #if SRML
            if (SRModLoader.IsModPresent("holyslimes") &&
                TryGetComponent(AccessTools.TypeByName("HolySlimes.Behaviors.SpiritHealOrDrain"), out var spirit))
            {
                SpiritModdedBehavior = gameObject.AddComponent<NetworkSpiritSlime>();
                SpiritModdedBehavior.HealOrDrainComponent = spirit;
            }
            #endif
        }

        private void OnEnable()
        {
            if (Owner == 0)
            {
                TakeOwnership();
            }
        }

        private void OnDisable()
        {
            if (IsLocal)
            {
                DropOwnership();
            }
        }

        private void Update()
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded || ID == 0) return;
            if (Owner == 0 && Globals.ClientLoaded) TakeOwnership();
            if (IsLocal)
            {
                m_MovementUpdateTimer -= Time.deltaTime;
                if (m_MovementUpdateTimer <= 0)
                {
                    m_MovementUpdateTimer = 0.1f;
                    if (Time.unscaledTime - m_LastPositionSent >= 1f || Vector3.Distance(m_ActualPosition, transform.position) > 0.1f || Quaternion.Angle(m_ActualRotation, transform.rotation) > 0.1f)
                    {
                        m_LastPositionSent = Time.unscaledTime;
                        m_LatestPosition = transform.position;
                        m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                        m_PreviousPosition = m_LatestPosition;
                        m_ActualPosition = m_LatestPosition;
                        m_LatestRotation = transform.rotation;
                        m_PreviousRotation = m_LatestRotation;
                        m_ActualRotation = m_LatestRotation;

                        ActorPositionBatcher.Enqueue(ID, transform.position, transform.rotation);
                    }

                    if (SlimeEat != null && SlimeEat.emotions != null &&
                        (!Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.AGITATION), m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION], 0.1f) ||
                        !Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.FEAR), m_PreviousEmotions[SlimeEmotions.Emotion.FEAR], 0.1f) ||
                        !Utils.CloseEnoughForMe(SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.HUNGER), m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER], 0.1f)))
                    {
                        m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.AGITATION);
                        m_PreviousEmotions[SlimeEmotions.Emotion.FEAR] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.FEAR);
                        m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER] = SlimeEat.emotions.GetCurr(SlimeEmotions.Emotion.HUNGER);

                        new PacketActorEmotions()
                        {
                            ID = ID,
                            Fear = m_PreviousEmotions[SlimeEmotions.Emotion.FEAR],
                            Agitation = m_PreviousEmotions[SlimeEmotions.Emotion.AGITATION],
                            Hunger = m_PreviousEmotions[SlimeEmotions.Emotion.HUNGER]
                        }.Send();
                    }

#if SRML
                    if (SpiritModdedBehavior != null)
                    {
                        // SpiritModdedBehavior.UpdatePacket();
                    }
#endif
                }
            }
            else
            {
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                }

                float t = 1.0f - ((m_LatestPositionTime - Time.time) / m_InterpolationPeriod);
                m_ActualPosition = Vector3.Lerp(m_PreviousPosition, m_LatestPosition, t);
                transform.position = m_ActualPosition;

                m_ActualRotation = Quaternion.Slerp(m_PreviousRotation, m_LatestRotation, t);
                transform.rotation = m_ActualRotation;
            }
        }

        public void PositionRotationUpdate(Vector3 pos, Quaternion rot, bool immediate)
        {
            // A packet may be dequeued just as an actor is being retired. Never
            // touch Unity transforms through a managed wrapper whose native
            // object has already been destroyed.
            if (this == null || gameObject == null) return;

            if (Vector3.Distance(pos, m_ActualPosition) > 10f) // Teleport detection
            {
                immediate = true;
            }
            if (!immediate)
            {
                m_LatestPosition = pos;
                m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                m_PreviousPosition = m_ActualPosition;

                m_LatestRotation = rot;
                m_PreviousRotation = m_ActualRotation;
            }
            else
            {
                m_LatestPosition = pos;
                m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                m_PreviousPosition = pos;
                m_ActualPosition = pos;
                transform.position = m_ActualPosition;

                m_PreviousRotation = rot;
                m_LatestRotation = rot;
                m_ActualRotation = rot;
                transform.rotation = rot;
            }

            RegionMember regionMember = m_RegionMember;
            if (regionMember != null && regionMember.gameObject != null && regionMember.hibernating)
            {
                regionMember.UpdateRegionMembership(true);
            }
        }

        public void TakeOwnership(bool interaction = false)
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded || ID == 0 ||
                Globals.HandlePacket || m_OwnershipRequested || IsLocal) return;
            if (!interaction && Owner != 0) return;
            if (Globals.IsServer)
            {
                SetOwnership(Globals.LocalID);
            }
            else m_OwnershipRequested = true;

            new PacketActorOwner()
            {
                ID = ID,
                Owner = Globals.LocalID,
                Interaction = interaction
            }.Send(NetDeliveryMethod.ReliableOrdered);
        }

        public void DropOwnership()
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded || Globals.HandlePacket || ID == 0 || !IsLocal) return;
            if (Globals.IsServer) SetOwnership(0);

            new PacketActorOwner()
            {
                ID = ID,
                Owner = 0
            }.Send();
        }

        public void SetOwnership(byte id)
        {
            m_OwnershipRequested = false;
            Owner = id;

            if (Owner != Globals.LocalID)
            {
                if (SRSingleton<SceneContext>.Instance != null && SRSingleton<SceneContext>.Instance.Player != null)
                {
                    var weapon = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();
                    if (weapon != null && weapon.held != null && weapon.held.GetComponent<NetworkActor>() != null && weapon.held.GetComponent<NetworkActor>().ID == ID)
                    {
                        weapon.DropAllVacced();
                    }
                }
            }
            else
            {
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                }
            }

        }

        internal PacketActors.ActorData CreateSnapshotData()
        {
            return new PacketActors.ActorData
            {
                ID = ID,
                Ident = Ident,
                RegionSet = RegionSet,
                Owner = Owner,
                Position = transform.position,
                Rotation = transform.rotation,
                AnimalModel = Reproduce != null ? Reproduce.model : null,
                PlortDestroyTime = DestroyPlortAfterTime != null && DestroyPlortAfterTime.plortModel != null
                    ? (double?)DestroyPlortAfterTime.plortModel.destroyTime : null,
                SlimeModel = SlimeEat != null ? SlimeEat.slimeModel : null,
                ProduceModel = ResourceCycle != null ? ResourceCycle.model : null,
#if SRML
                NicknameModValue = Globals.NicknamesModInstalled
                    ? NetworkNickname.GetName(this, "Nicknames.SlimeNickname") : null,
#endif
            };
        }

        internal void OnDestroyEffect()
        {
            if (this == null || gameObject == null) return;

            var slimeKey = GetComponentInChildren<SlimeKey>();
            if (slimeKey != null)
            {
                if (slimeKey.pickupFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(slimeKey.pickupFX, slimeKey.transform.position, slimeKey.transform.rotation);
                }
            }
            var destroyOnTouching = GetComponentInChildren<DestroyOnTouching>();
            if (destroyOnTouching != null)
            {
                if (destroyOnTouching.destroyFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(destroyOnTouching.destroyFX, destroyOnTouching.transform.position, destroyOnTouching.transform.rotation);
                }
            }
            var exchangeBreakOnImpact = GetComponentInChildren<ExchangeBreakOnImpact>();
            if (exchangeBreakOnImpact != null)
            {
                SRMP.Log("Exchange Box Broke!", "ACTOR");
                SRBehaviour.SpawnAndPlayFX(exchangeBreakOnImpact.breakFX, exchangeBreakOnImpact.gameObject.transform.position, exchangeBreakOnImpact.gameObject.transform.rotation);
            }
            var breakOnImpact = GetComponentInChildren<BreakOnImpactBase>();
            if (breakOnImpact != null)
            {
                SRBehaviour.SpawnAndPlayFX(breakOnImpact.breakFX, breakOnImpact.gameObject.transform.position, breakOnImpact.gameObject.transform.rotation);
            }
            var quicksilver = GetComponentInChildren<QuicksilverPlortCollector>();
            if (quicksilver != null)
            {
                if (quicksilver.destroyFX != null)
                {
                    SRBehaviour.SpawnAndPlayFX(quicksilver.destroyFX, quicksilver.transform.position, Quaternion.identity);
                }
                SECTR_AudioSystem.Play(quicksilver.onCollectionCue, quicksilver.transform.position, false);
            }
        }
    }
}
