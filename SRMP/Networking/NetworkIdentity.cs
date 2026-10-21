using System;
using System.Security.Cryptography;
using System.Text;

namespace SRMultiplayer.Networking
{
    internal static class NetworkIdentity
    {
        internal static bool IsSameProductUser(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.Ordinal);
        }

        internal static string InventoryFile(Guid host, string world)
        {
            if (host == Guid.Empty || string.IsNullOrWhiteSpace(world))
                throw new ArgumentException("A stable host and world identity are required.");
            using (var hash = SHA256.Create())
                return "player-" + host.ToString("N") + "-" +
                    BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(world))).Replace("-", "") + ".player";
        }

        // Save identity is derived on the server from the authenticated EOS
        // connection. A client supplied Guid must never select whose position
        // file the host reads or overwrites.
        internal static Guid ForPlayerSave(string authenticatedProductUserId)
        {
            if (string.IsNullOrWhiteSpace(authenticatedProductUserId))
                throw new ArgumentException("An authenticated product user ID is required.", nameof(authenticatedProductUserId));

            using (var hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(Encoding.UTF8.GetBytes(
                    "SRMP.PlayerSave.v1\0" + authenticatedProductUserId));
                var guidBytes = new byte[16];
                Buffer.BlockCopy(digest, 0, guidBytes, 0, guidBytes.Length);
                return new Guid(guidBytes);
            }
        }

        // A specified hash is identical across processes and Mono versions;
        // String.GetHashCode is deliberately not a network identity contract.
        internal static int ForScenePath(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in path) hash = (hash ^ c) * 16777619;
                return (int)hash;
            }
        }

        internal static int ForActor(byte creator, int sequence)
        {
            if (creator == 0 || sequence <= 0 || sequence > 0xFFFFFF)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            return unchecked((int)(((uint)creator << 24) | (uint)sequence));
        }
    }
}
