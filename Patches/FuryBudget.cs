using HarmonyLib;
using UnityEngine;
using GlobalEnums;
using System.Runtime.CompilerServices;
using System;

[HarmonyPatch]
internal static class FuryBudget {
    private const float RAGE_EFFECT_TIME_CONSTANT = 10f;
    private const float RAGE_HEAL_TIME_CONSTANT = 11f;
    private enum AttackType
    { None, Slash, Downspike, Custom }

    private static int budget = 0;
    private static AttackType pendingAttack = AttackType.None;

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HeroController), "ResetWarriorCrestState")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallResetWarriorCrestState(HeroController instance)
    {
        throw new NotImplementedException("Reverse patch not applied for ResetWarriorCrestState");
    }

    private static void EndFury(HeroController instance)
    {

        if (!instance.WarriorState.IsInRageMode) {
            return;
        }

        pendingAttack = AttackType.None;

        Debug.Log(
            $"[BeastRework] Fury ending"
        );

        CallResetWarriorCrestState(instance);
    }

    [HarmonyPatch(typeof(HeroController), "NailHitEnemy")]
    [HarmonyPostfix]
    private static void OnNailHitEnemy(ref HeroController.WarriorCrestStateInfo ___warriorState) {
        // Reset countdown timer
        if (___warriorState.IsInRageMode)
        {
            Debug.Log($"[BeastRework] Nail attack hit. Extending rage timeout. Prev | ${___warriorState.RageEffectTimeLeft} : New | ${RAGE_EFFECT_TIME_CONSTANT}");
            ___warriorState.RageEffectTimeLeft = RAGE_EFFECT_TIME_CONSTANT;
            ___warriorState.RageHealTimeLeft = RAGE_HEAL_TIME_CONSTANT;
        }

    }

    [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
    [HarmonyPostfix]
    private static void OnHeroDamaged(int amount)
    {
        Debug.Log("[BeastRework] Hero Damage detected. Grabbing rage instance");
        var instance = HeroController.instance;
        if (!instance.WarriorState.IsInRageMode || budget <= 0 || amount <= 0)
            return;

        budget--;

        Debug.Log($"[BeastRework] Damage penalty | budget={budget}");

        if (budget == 0)
            EndFury(instance);
    }


    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    [HarmonyPostfix]
    private static void OnBindCompleted(ref HeroController.WarriorCrestStateInfo ___warriorState)
    {
        if (!___warriorState.IsInRageMode) return;
        budget = 8;
        Debug.Log($"[BeastRework] Attack budget set to 8");

        ___warriorState.RageEffectTimeLeft = RAGE_EFFECT_TIME_CONSTANT;
        ___warriorState.RageHealTimeLeft = RAGE_HEAL_TIME_CONSTANT;

        Debug.Log($"[BeastRework] TODO: REMOVE - Rage Time extended to 10 for testing");
    }

    [HarmonyPatch(typeof(HeroController), "Attack")]
    [HarmonyPostfix]
    private static void OnAttack(HeroController __instance, AttackDirection attackDir)
    {

        if (__instance.WarriorState.IsInRageMode)
        {
            if (budget == 0)
            {
                Debug.Log("[BeastRework] Budget already expired");
                return;
            }

            budget--;
            Debug.Log($"[BeastRework] Budget " + budget);

            if (budget == 0)
            {
                if (attackDir == AttackDirection.downward)
                {
                    switch (__instance.Config.DownSlashType)
                    {
                        case HeroControllerConfig.DownSlashTypes.DownSpike:
                            pendingAttack = AttackType.Downspike;
                            break;

                        case HeroControllerConfig.DownSlashTypes.Custom:
                            pendingAttack = AttackType.Custom;
                            break;

                        case HeroControllerConfig.DownSlashTypes.Slash:
                            pendingAttack = AttackType.Slash;
                            break;
                    }
                }
                else
                {
                    pendingAttack = AttackType.Slash;
                }

                Debug.Log(
                    $"[BeastRework] Final attack registered: {pendingAttack}"
                );
            }
        }
    }

    [HarmonyPatch(typeof(HeroController), "ResetAttacks")]
    [HarmonyPostfix]
    private static void OnResetAttacks(HeroController __instance)
    {
        if (__instance.WarriorState.IsInRageMode && budget <= 0 && pendingAttack == AttackType.Slash)
        {
            // Terminate Rage
            Debug.Log("[BeastRework] Reset end attack detected");
            EndFury(__instance);
        }
    }

    [HarmonyPatch(typeof(HeroController), "FinishDownspike", new[] { typeof(bool) })]
    [HarmonyPostfix]
    private static void OnFinishDownspike(HeroController __instance)
    {
        if (__instance.WarriorState.IsInRageMode && budget <= 0 && pendingAttack == AttackType.Downspike)
        {
            // Terminate Rage
            Debug.Log("[BeastRework] Downspike end attack detected");
            EndFury(__instance);
        }
    }

    [HarmonyPatch(typeof(HeroController), "CancelAttack", new[] {typeof(bool)})]
    [HarmonyPostfix]
    private static void OnCancelAttack(HeroController __instance) {
        if (__instance.WarriorState.IsInRageMode && budget <= 0)
        {
            Debug.Log("[BeastRework] Cancel end attack detected");
            EndFury(__instance);
        }
    }
}
