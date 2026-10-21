using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonomiPark.SlimeRancher.Regions;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkPlayerDisplayOnMap : DisplayOnMap
    {
        public NetworkPlayer Player;
        public RegionMember Member;

        public override void Awake()
        {
            base.Awake();

            if (SRSingleton<Map>.Instance.mapUI.gameObject.activeInHierarchy)
            {
                MapMarker marker = GetMarker();
                marker.transform.SetParent(SRSingleton<Map>.Instance.mapUI.mapMarkerSection.gameObject.transform, false);
                marker.transform.localPosition = Vector3.zero;
            }
        }

        private void Update()
        {
            if(SRSingleton<Map>.Instance.mapUI.gameObject.activeInHierarchy)
            {
                if (ShowOnMap())
                {
                    MapMarker marker = GetMarker();
                    marker.gameObject.SetActive(true);
                    RegionRegistry.RegionSetId regionSetId = GetRegionSetId();
                    Vector4 coefficients;
                    Vector2 minPoint;
                    Vector2 maxPoint;
                    float num;
                    if (regionSetId == RegionRegistry.RegionSetId.DESERT)
                    {
                        coefficients = SRSingleton<Map>.Instance.mapUI.desertCoefficients;
                        minPoint = SRSingleton<Map>.Instance.mapUI.desertMarkerPositionMin;
                        maxPoint = SRSingleton<Map>.Instance.mapUI.desertMarkerPositionMax;
                        num = SRSingleton<Map>.Instance.mapUI.desertRotationAdjustment;
                    }
                    else
                    {
                        coefficients = SRSingleton<Map>.Instance.mapUI.mainCoefficients;
                        minPoint = SRSingleton<Map>.Instance.mapUI.worldMarkerPositionMin;
                        maxPoint = SRSingleton<Map>.Instance.mapUI.worldMarkerPositionMax;
                        num = SRSingleton<Map>.Instance.mapUI.mainRotationAdjustment;
                    }
                    marker.SetAnchoredPosition(SRSingleton<Map>.Instance.mapUI.GetMapPosClamped(GetCurrentPosition(), coefficients, minPoint, maxPoint));
                    Vector3 eulerAngles = GetCurrentRotation().eulerAngles;
                    marker.Rotate(Quaternion.Euler(eulerAngles.x + num, eulerAngles.y, eulerAngles.z));
                }
                else
                {
                    GetMarker().gameObject.SetActive(false);
                }
            }
        }

        public override ZoneDirector.Zone GetZoneId()
        {
            return GetCurrentZone();
        }

        public override RegionRegistry.RegionSetId GetRegionSetId()
        {
            return Player.CurrentRegionSet;
        }

        public override Vector3 GetCurrentPosition()
        {
            return Player.transform.position;
        }

        public override Quaternion GetCurrentRotation()
        {
            return Player.transform.rotation;
        }

        public override bool ShowOnMap()
        {
            return playerState.HasUnlockedMap(GetZoneId());
        }

        public ZoneDirector.Zone GetCurrentZone()
        {
            if (ZoneDirector.Zones(Member).Count == 0)
            {
                return ZoneDirector.Zone.NONE;
            }
            ZoneDirector.Zone result = ZoneDirector.Zone.NONE;
            int num = int.MaxValue;
            foreach (ZoneDirector.Zone zone in ZoneDirector.Zones(Member))
            {
                if (zone < (ZoneDirector.Zone)num)
                {
                    result = zone;
                    num = (int)zone;
                }
            }
            return result;
        }

        private bool IsInHiddenCell()
        {
            return (from r in Member.regions
                    where r.cellDir.notShownOnMap
                    select r).Count<Region>() > 0;
        }
    }
}