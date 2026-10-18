
using HarmonyLib;
using BepInEx.Logging;

namespace BeastRework.Patches
{
    [HarmonyPatch(
        typeof(GameManager),
        "SetLoadedGameData",
        typeof(SaveGameData),
        typeof(int))]
    internal static class GameLoadPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Plugin.Log.LogInfo(
                "Harmony: Game save loaded successfully!");
        }
    }
}
