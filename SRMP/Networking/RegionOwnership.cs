using System.Collections.Generic;
using System.Linq;

namespace SRMultiplayer.Networking
{
    /// <summary>Deterministic region-authority handoff policy, independent of Unity.</summary>
    internal static class RegionOwnership
    {
        internal static byte SelectReplacementOwner(byte hostId, byte leavingId, IEnumerable<byte> connectedPlayersInRegion)
        {
            var candidates = (connectedPlayersInRegion ?? Enumerable.Empty<byte>())
                .Where(id => id != 0 && id != leavingId)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            if (hostId != 0 && candidates.Contains(hostId)) return hostId;
            return candidates.Length == 0 ? (byte)0 : candidates[0];
        }
    }
}
