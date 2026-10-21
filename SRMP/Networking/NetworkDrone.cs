using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkDrone : MonoBehaviour
    {
        public Drone Drone;
        public NetworkRegion Region;

        public bool IsLocal { get { return Region != null && Region.IsLocal; } }

        private RegionMember m_RegionMember;
        private float m_MovementUpdateTimer;
        private float m_LastPositionSent;
        private Vector3 m_ActualPosition;
        private Vector3 m_LatestPosition;
        private Vector3 m_PreviousPosition;
        private Quaternion m_ActualRotation;
        private Quaternion m_LatestRotation;
        private Quaternion m_PreviousRotation;
        private float m_LatestPositionTime;
        private float m_InterpolationPeriod = 0.1f;

        private void Awake()
        {
            m_RegionMember = GetComponent<RegionMember>();
        }

        private void OnDestroy()
        {
            if (Region != null) Region.OnBecameOwner -= OnBecameOwner;
        }

        private void OnBecameOwner()
        {
            if (Drone != null && Drone.plexer != null) Drone.plexer.ForceRethink();
        }

        private void Start()
        {
            if (Region == null) Region = gameObject.GetInParent<NetworkRegion>();
            if (Region != null) Region.OnBecameOwner += OnBecameOwner;
            if (IsLocal) OnBecameOwner();

            m_LatestPosition = transform.position;
            m_LatestPositionTime = Time.time + m_InterpolationPeriod;
            m_PreviousPosition = m_LatestPosition;
            m_ActualPosition = m_LatestPosition;
            m_LatestRotation = transform.rotation;
            m_PreviousRotation = m_LatestRotation;
            m_ActualRotation = m_LatestRotation;
        }

        private void Update()
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded || Drone == null || Drone.droneModel == null) return;
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

                        m_MovementUpdateTimer = 0.1f;
                        new PacketDronePosition()
                        {
                            ID = Drone.droneModel.siteId,
                            Position = transform.position,
                            Rotation = transform.rotation
                        }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
                    }
                }
            }
            else
            {
                float t = 1.0f - ((m_LatestPositionTime - Time.time) / m_InterpolationPeriod);
                m_ActualPosition = Vector3.Lerp(m_PreviousPosition, m_LatestPosition, t);
                transform.position = m_ActualPosition;

                m_ActualRotation = Quaternion.Slerp(m_PreviousRotation, m_LatestRotation, t);
                transform.rotation = m_ActualRotation;
            }
        }

        public void PositionRotationUpdate(Vector3 pos, Quaternion rot, bool immediate)
        {
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

            if (m_RegionMember != null && m_RegionMember.hibernating)
            {
                m_RegionMember.UpdateRegionMembership(true);
            }
        }
    }
}
