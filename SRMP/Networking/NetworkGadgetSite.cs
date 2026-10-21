using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkGadgetSite : MonoBehaviour
    {
        public GadgetSite Site;
        public NetworkRegion Region;

        public bool IsLocal { get { return Region != null && Region.IsLocal; } }

        private HydroTurret[] m_Turrets;
        private Quaternion[] m_ActualRotations;
        private Quaternion[] m_PreviousRotations;
        private Quaternion[] m_LatestRotations;
        private float m_LatestPositionTime;
        private float m_InterpolationPeriod = 0.1f;
        private GameObject m_TurretGadget;
        private float m_NextUpdate;
        private float m_LastSent;

        private bool EnsureTurrets()
        {
            if (Site == null || !Site.HasAttached() ||
                (Site.GetAttachedId() != Gadget.Id.HYDRO_TURRET && Site.GetAttachedId() != Gadget.Id.SUPER_HYDRO_TURRET))
            {
                m_Turrets = null;
                m_TurretGadget = null;
                return false;
            }
            if (m_Turrets != null && m_TurretGadget == Site.attached) return true;
            m_TurretGadget = Site.attached;
            m_Turrets = Site.gameObject.GetComponentsInChildren<HydroTurret>(true);
            m_ActualRotations = new Quaternion[m_Turrets.Length];
            m_PreviousRotations = new Quaternion[m_Turrets.Length];
            m_LatestRotations = new Quaternion[m_Turrets.Length];
            for (int i = 0; i < m_Turrets.Length; i++)
                m_ActualRotations[i] = m_PreviousRotations[i] = m_LatestRotations[i] = m_Turrets[i].toRotate.rotation;
            return true;
        }

        private void Update()
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded) return;
            if (EnsureTurrets())
            {
                if(IsLocal)
                {
                    if (Time.unscaledTime < m_NextUpdate) return;
                    m_NextUpdate = Time.unscaledTime + m_InterpolationPeriod;
                    if(AnyChange() || Time.unscaledTime - m_LastSent >= 1f)
                    {
                        m_LastSent = Time.unscaledTime;
                        m_LatestPositionTime = Time.time + m_InterpolationPeriod;
                        var data = new List<PacketGadgetTurrets.TurretData>();
                        for (int i = 0; i < m_Turrets.Length; i++)
                        {
                            m_ActualRotations[i] = m_Turrets[i].toRotate.rotation;
                            m_PreviousRotations[i] = m_Turrets[i].toRotate.rotation;
                            m_LatestRotations[i] = m_Turrets[i].toRotate.rotation;
                            data.Add(new PacketGadgetTurrets.TurretData()
                            {
                                Index = i,
                                Rotation = m_Turrets[i].toRotate.rotation
                            });
                        }
                        new PacketGadgetTurrets()
                        {
                            ID = Site.id,
                            Turrets = data
                        }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
                    }
                }
                else
                {
                    float t = 1.0f - ((m_LatestPositionTime - Time.time) / m_InterpolationPeriod);
                    for (int i = 0; i < m_Turrets.Length; i++)
                    {
                        m_ActualRotations[i] = Quaternion.Slerp(m_PreviousRotations[i], m_LatestRotations[i], t);
                        m_Turrets[i].toRotate.rotation = m_ActualRotations[i];
                    }
                }
            }
            else
            {
                m_Turrets = null;
            }
        }

        private bool AnyChange()
        {
            for(int i = 0; i < m_Turrets.Length; i++)
            {
                if(Quaternion.Angle(m_ActualRotations[i], m_Turrets[i].toRotate.rotation) > 0.1f)
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateTurretRotation(int index, Quaternion rotation)
        {
            if (!EnsureTurrets() || index < 0 || index >= m_Turrets.Length) return;
            m_LatestPositionTime = Time.time + m_InterpolationPeriod;
            m_LatestRotations[index] = rotation;
            m_PreviousRotations[index] = m_ActualRotations[index];
        }
    }
}
