
using BepInEx;
using HarmonyLib;
using BepInEx.Logging;


namespace BeastRework
{
    [BepInPlugin(
        "ciro.silksong.beastrework",
        "Beast Rework",
        "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony? _harmony;
        internal static ManualLogSource Log = null!;

        private void Awake()
        {

            Log = Logger;
            Logger.LogInfo("Beast Rework initialized!");

            _harmony = new Harmony(
                "ciro.silksong.beastrework");

            _harmony.PatchAll();

            Logger.LogInfo("Harmony patches registered.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
