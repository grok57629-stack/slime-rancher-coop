using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer
{
    /// <summary>
    /// Used marking the game time movement status 
    /// </summary>
    public enum PauseState
    {
        Pause,
        Playing
    }

    /// <summary>
    /// Handles basic user data to be used for connection communication
    /// Uesr ID, mod count and dlc checkers
    /// </summary>
    [Serializable]
    public struct UserData
    {
        public Guid UUID;
        public bool CheckDLC;
        public List<string> IgnoredMods;
    }
}
