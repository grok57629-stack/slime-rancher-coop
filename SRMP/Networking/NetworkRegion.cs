using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkRegion : MonoBehaviour
    {
        public Region Region;
        public RanchCellFastForwarder FastForwarder;

        public int ID;
        public byte Owner;
        public bool IsLocal { get { return Globals.IsMultiplayer && Owner != 0 && Owner == Globals.LocalID; } }
        private bool ownershipRequested;
        public List<NetworkPlayer> Players = new List<NetworkPlayer>();
        public Action OnBecameOwner;

        public void AddPlayer(NetworkPlayer player)
        {
            if (player == null || Players.Contains(player)) return;
            if (Players.Count == 0 && FastForwarder != null)
            {
                bool wasHandling = Globals.HandlePacket;
                try
                {
                    Globals.HandlePacket = false;
                    SRSingleton<GameContext>.Instance.StartCoroutine(FastForwarder.OnFastForward());
                }
                finally { Globals.HandlePacket = wasHandling; }
            }

            Players.Add(player);
            player.Regions.Add(this);
            if (Globals.IsServer)
                NetworkServer.Instance?.TrackRegionPlayer(ID, player.ID, true);
            if (player.IsLocal) return;
            new PacketRegionOwner { ID = ID, Owner = Owner }.Send(player);
            var netActors = Region.members.Data.Select(m => m?.GetComponent<NetworkActor>())
                .Where(a => a != null && !a.KnownPlayers.Contains(player)).ToList();
            foreach (var netActor in netActors)
            {
                netActor.KnownPlayers.Add(player);
            }

            new PacketActors()
            {
                Actors = netActors.Select(a => a.CreateSnapshotData()).ToList()
            }.Send(player);
            //SRMP.Log($"{player} loaded into {name} with {netActors.Count} unknown network actors ({Region.members.GetCount()} actors total)");

        }

        public void RemovePlayer(NetworkPlayer player)
        {
            if (Players.Contains(player))
            {
                Players.Remove(player);
                player.Regions.Remove(this);
                if (Globals.IsServer)
                    NetworkServer.Instance?.TrackRegionPlayer(ID, player.ID, false);
                //SRMP.Log($"{player} unloaded {name}");

                if (Players.Count == 0 && FastForwarder != null)
                {
                    FastForwarder.model.hibernationTime = new double?(SRSingleton<SceneContext>.Instance.TimeDirector.WorldTime());
                }
            }
        }

        public void TakeOwnership()
        {
            if (!Globals.IsMultiplayer || !Globals.GameLoaded || Globals.HandlePacket || ownershipRequested || IsLocal) return;
            if (Globals.IsServer) SetOwnership(Globals.LocalID);
            else ownershipRequested = true;

            new PacketRegionOwner()
            {
                ID = ID,
                Owner = Globals.LocalID
            }.Send(NetDeliveryMethod.ReliableOrdered);
            
        }

        public void DropOwnership()
        {
            if (!IsLocal || Globals.HandlePacket) return;
            if (Globals.IsServer) SetOwnership(0);

            new PacketRegionOwner()
            {
                ID = ID,
                Owner = 0
            }.Send();
        }

        public void SetOwnership(byte id)
        {
            ownershipRequested = false;
            if (Owner == id)
                return;
            
            Owner = id;
            if (Globals.IsServer)
                NetworkServer.Instance?.TrackRegionOwner(ID, id);
            //SRMP.Log($"Owner for {name} now {(Owner == 0 ? "None" : Globals.Players[Owner].ToString())}");

            if(IsLocal)
            {
                OnBecameOwner?.Invoke();
            }
        }

        private void Update()
        {
            if (Globals.ClientLoaded && Owner == 0 && Region != null && Region.root.activeInHierarchy)
                TakeOwnership();
        }
    }
}
