using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer
{
    public static class Utils
    {
        internal static void LoadUserData()
        {
            string path = Path.Combine(SRMP.ModDataPath, "userdata.json");
            var data = new UserData { CheckDLC = true, IgnoredMods = new List<string>() };
            bool save = !File.Exists(path);
            if (!save)
            {
                try
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
                    data = json.ToObject<UserData>();
                    if (json["CheckDLC"] == null) { data.CheckDLC = true; save = true; }
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    // Keep the damaged input for diagnosis; never silently erase it.
                    File.Copy(path, path + ".invalid-" + Guid.NewGuid().ToString("N"));
                    SRMP.Log($"[Config] Invalid userdata preserved: {ex.Message}");
                    string backup = path + ".bak";
                    if (File.Exists(backup))
                    {
                        try { data = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(File.ReadAllText(backup)); }
                        catch (Newtonsoft.Json.JsonException backupError)
                        { SRMP.Log($"[Config] Backup could not be read: {backupError.Message}"); }
                    }
                    save = true;
                }
            }
            if (data.UUID == Guid.Empty) { data.UUID = Guid.NewGuid(); save = true; }
            if (data.IgnoredMods == null) { data.IgnoredMods = new List<string>(); save = true; }
            Globals.UserData = data;
            if (save) AtomicFile.WriteText(path, Newtonsoft.Json.JsonConvert.SerializeObject(data));
            SRMP.Log("Loaded userdata with UUID " + data.UUID);
        }

        /// <summary>
        /// Loads up any resources embedded in the mod
        /// </summary>
        /// <param name="filename">Name of resource</param>
        /// <returns>Contents of resource</returns>
        public static byte[] ExtractResource(String filename)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream resFilestream = a.GetManifestResourceStream(filename))
            {
                if (resFilestream == null) return null;
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                return ba;
            }
        }
        /// <summary>
        /// Sets what layer the given item is at
        /// </summary>
        public static void SetLayer(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }
        /// <summary>
        /// Check to see if the provided values are close enough to be the same
        /// </summary>
        public static bool CloseEnoughForMe(double value1, double value2, double acceptableDifference)
        {
            return Math.Abs(value1 - value2) <= acceptableDifference;
        }
        /// <summary>
        /// Check to see if the provided values are close enough to be the same
        /// </summary>
        public static bool CloseEnoughForMe(float value1, float value2, float acceptableDifference)
        {
            return Mathf.Abs(value1 - value2) <= acceptableDifference;
        }

        internal static int ActorSequence;

        public static int GetRandomActorID()
        {
            // The host supplies the cursor at authentication when a player id
            // is reused; peers allocate disjoint namespaces without collisions.
            int id;
            do { id = Networking.NetworkIdentity.ForActor(Globals.LocalID, ++ActorSequence); }
            while (Globals.Actors.ContainsKey(id));
            return id;
        }

        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        const long byteConversion = 1000;
        /// <summary>
        /// Takes a count of bytes and turns it into a human readable version
        /// </summary>
        /// <param name="value">Count of Bytes as a long</param>
        /// <returns>String statement of the bytes </returns>
        public static string GetHumanReadableFileSize(long value)
        {

            if (value < 0) { return "-" + GetHumanReadableFileSize(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, byteConversion);
            double adjustedSize = (value / Math.Pow(1000, mag));


            return string.Format("{0:n2} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        /// <summary>
        /// Gets a value that indicates whether <paramref name="path"/>
        /// is a valid path.
        /// </summary>
        /// <returns>Returns <c>true</c> if <paramref name="path"/> is a
        /// valid path; <c>false</c> otherwise. Also returns <c>false</c> if
        /// the caller does not have the required permissions to access
        /// <paramref name="path"/>.
        /// </returns>
        /// <seealso cref="Path.GetFullPath"/>
        /// <seealso cref="TryGetFullPath"/>
        public static bool IsValidPath(string path)
        {
            string result;
            return TryGetFullPath(path, out result);
        }

        /// <summary>
        /// Returns the absolute path for the specified path string. A return
        /// value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="path">The file or directory for which to obtain absolute
        /// path information.
        /// </param>
        /// <param name="result">When this method returns, contains the absolute
        /// path representation of <paramref name="path"/>, if the conversion
        /// succeeded, or <see cref="String.Empty"/> if the conversion failed.
        /// The conversion fails if <paramref name="path"/> is null or
        /// <see cref="String.Empty"/>, or is not of the correct format. This
        /// parameter is passed uninitialized; any value originally supplied
        /// in <paramref name="result"/> will be overwritten.
        /// </param>
        /// <returns><c>true</c> if <paramref name="path"/> was converted
        /// to an absolute path successfully; otherwise, false.
        /// </returns>
        /// <seealso cref="Path.GetFullPath"/>
        /// <seealso cref="IsValidPath"/>
        public static bool TryGetFullPath(string path, out string result)
        {
            result = String.Empty;
            if (String.IsNullOrWhiteSpace(path)) { return false; }
            bool status = false;

            try
            {
                result = Path.GetFullPath(path);
                status = true;
            }
            catch (ArgumentException) { }
            catch (SecurityException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }

            return status;
        }
    }
}
