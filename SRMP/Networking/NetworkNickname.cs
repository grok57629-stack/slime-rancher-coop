using HarmonyLib;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    /// <summary>Reads optional nickname-mod data without making joins depend on every actor having a nickname component.</summary>
    internal static class NetworkNickname
    {
        internal static string GetName(Component target, string componentTypeName)
        {
            if (target == null || string.IsNullOrEmpty(componentTypeName)) return null;

            var componentType = AccessTools.TypeByName(componentTypeName);
            if (componentType == null) return null;

            var nickname = target.GetComponent(componentType);
            if (nickname == null) return null;

            var nameField = AccessTools.Field(componentType, "Name");
            return nameField == null ? null : nameField.GetValue(nickname) as string;
        }

        internal static void SetName(Component target, string componentTypeName, string value)
        {
            if (target == null || string.IsNullOrEmpty(componentTypeName) || value == null) return;
            var componentType = AccessTools.TypeByName(componentTypeName);
            if (componentType == null) return;
            var nickname = target.GetComponent(componentType);
            var nameField = AccessTools.Field(componentType, "Name");
            if (nickname != null && nameField != null && nameField.FieldType == typeof(string))
                nameField.SetValue(nickname, value);
        }
    }
}
