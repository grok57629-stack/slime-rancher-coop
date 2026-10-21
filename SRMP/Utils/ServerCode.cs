using System;
using System.Security.Cryptography;
using System.Text;

namespace SRMultiplayer
{
    /// <summary>
    /// The shareable lobby code format. Keep generation, UI and EOS validation
    /// on the same length and alphabet so a displayed code is always joinable.
    /// </summary>
    internal static class ServerCode
    {
        internal const int Length = 7;
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        internal static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var normalized = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsWhiteSpace(character) || character == '-') continue;
                normalized.Append(char.ToUpperInvariant(character));
            }
            return normalized.ToString();
        }

        internal static bool IsValid(string value)
        {
            if (value == null || value.Length != Length) return false;
            foreach (char character in value)
            {
                if ((character < 'A' || character > 'Z') && (character < '0' || character > '9'))
                    return false;
            }
            return true;
        }

        internal static string Generate()
        {
            // Rejection sampling avoids modulo bias while using only APIs
            // available on the game's .NET Framework target.
            var result = new char[Length];
            var bytes = new byte[Length * 2];
            using (var random = RandomNumberGenerator.Create())
            {
                int filled = 0;
                while (filled < result.Length)
                {
                    random.GetBytes(bytes);
                    foreach (byte candidate in bytes)
                    {
                        if (candidate >= 252) continue; // 7 * 36, evenly divisible into 256
                        result[filled++] = Alphabet[candidate % Alphabet.Length];
                        if (filled == result.Length) break;
                    }
                }
            }
            return new string(result);
        }
    }
}
