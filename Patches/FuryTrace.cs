
using GlobalEnums;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
internal static class FuryTrace
{
    private static bool effectEndLogged;

    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    [HarmonyPostfix]
    private static void OnBindCompleted(HeroController __instance)
    {
        var state = __instance.WarriorState;

        if (!state.IsInRageMode)
            return;

        effectEndLogged = false;

        Debug.Log(
            $"[BeastRework] FURY START | " +
            $"effect={state.RageEffectTimeLeft:F2} | " +
            $"heal={state.RageHealTimeLeft:F2}");
    }

    [HarmonyPatch(typeof(HeroController), "Attack")]
    [HarmonyPostfix]
    private static void OnAttack(
        HeroController __instance,
        AttackDirection attackDir)
    {
        var state = __instance.WarriorState;

        if (!state.IsInRageMode)
            return;

        Debug.Log(
            $"[BeastRework] ATTACK | " +
            $"direction={attackDir} | " +
            $"count={__instance.cState.attackCount} | " +
            $"effect={state.RageEffectTimeLeft:F2}");
    }

    [HarmonyPatch(typeof(HeroController), "NailHitEnemy")]
    [HarmonyPostfix]
    private static void OnNailHitEnemy(
        HeroController __instance,
        HitInstance hitInstance)
    {
        if (!hitInstance.RageHit)
            return;

        var state = __instance.WarriorState;

        Debug.Log(
            $"[BeastRework] RAGE HIT | " +
            $"attack={__instance.cState.attackCount} | " +
            $"firstHit={hitInstance.IsFirstHit} | " +
            $"heals={state.RageModeHealCount} | " +
            $"effect={state.RageEffectTimeLeft:F2}");
    }

    [HarmonyPatch(typeof(HeroController), "ResetWarriorRageEffect")]
    [HarmonyPrefix]
    private static void OnEffectReset(HeroController __instance)
    {
        var state = __instance.WarriorState;

        if (!state.IsInRageMode || effectEndLogged)
            return;

        effectEndLogged = true;

        Debug.Log(
            $"[BeastRework] FURY EFFECT RESET | " +
            $"effect={state.RageEffectTimeLeft:F2} | " +
            $"heal={state.RageHealTimeLeft:F2}");
    }

    [HarmonyPatch(typeof(HeroController), "ResetWarriorCrestState")]
    [HarmonyPrefix]
    private static void OnFuryCleanup(HeroController __instance)
    {
        var state = __instance.WarriorState;

        if (!state.IsInRageMode)
            return;

        Debug.Log(
            $"[BeastRework] FURY CLEANUP | " +
            $"effect={state.RageEffectTimeLeft:F2} | " +
            $"heal={state.RageHealTimeLeft:F2} | " +
            $"totalHeals={state.RageModeHealCount}");
    }
}


[HarmonyPatch(typeof(HeroController), "TakeDamage")]
internal static class TraceFuryDamage
{
    [HarmonyPrefix]
    private static void Prefix(
        HeroController __instance,
        out HeroController.WarriorCrestStateInfo __state)
    {
        __state = __instance.WarriorState;
    }

    [HarmonyPostfix]
    private static void Postfix(
        HeroController __instance,
        HeroController.WarriorCrestStateInfo __state)
    {
        if (!__state.IsInRageMode)
            return;

        var current = __instance.WarriorState;

        float penalty =
            __state.RageEffectTimeLeft -
            current.RageEffectTimeLeft;

        if (penalty <= 0f)
            return;

        UnityEngine.Debug.Log(
            $"[BeastRework] FURY DAMAGE PENALTY | " +
            $"secondsRemoved={penalty:F2} | " +
            $"effect={current.RageEffectTimeLeft:F2} | " +
            $"heal={current.RageHealTimeLeft:F2}");
    }
}
