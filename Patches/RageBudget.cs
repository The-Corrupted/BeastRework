using HarmonyLib;
using UnityEngine;
using GlobalEnums;
using System.Runtime.CompilerServices;
using System;

[HarmonyPatch]
internal static class RageBudget {
    private const float RAGE_EFFECT_TIME_CONSTANT = 10f;
    // The vanilla beast mechanics has a very short grace period between rage effect end and rage heal end where the user
    // can still get a healing strike. We're going to replicate this by having rage heal time be a second longer than rage
    // effect time
    private const float RAGE_HEAL_TIME_CONSTANT = 11f;
    // Silksongs default behavior for every heal hit to apply ~1s of replenishment
    // We're going to up this to 3s for heal hits and 2s for non-healing hits to provide enemy RNG
    // tolerance without giving the player a potential maximum duration of over a minute
    private const float RAGE_EFFECT_REPLENISHMENT = 2f;
    private const float RAGE_HEAL_REPLENISHMENT = 2f;
    // This isn't really used at the moment.
    private enum AttackType
    { None, Slash, Downspike, Custom }
    // To balance rage and provent the more generous timer from becoming an additional additional damage machine
    // we give the user an attackBudget that's approximately the same number of attacks it takes to regenerate
    // a new spool of thread. This allows optimial beast play to be chainable without becoming overtly overpowered
    private const int MAXIMUM_ATTACK_BUDGET = 8;
    private static int attackBudget = 0;
    private static AttackType pendingAttack = AttackType.None;
    // The rage effect can end before rage is actually reset which can result in UI bugs. We're going to keep
    // a guard that tells us if the rage effect has ended so we don't accidentally increment the effect after
    // the UI has been reset
    private static bool rageEffectActive = false;

    private static void EndRage(HeroController instance)
    {

        if (!instance.WarriorState.IsInRageMode)
        {
            return;
        }

        Debug.Log("[BeastRework] Rage crest reset. Clearing state");

        attackBudget = 0;
        pendingAttack = AttackType.None;
        rageEffectActive = false;
        CallResetWarriorCrestState(instance);
    }

    // Make ResetWarriorCrestState available to us so we can do a targeted reset of the warrior crest
    // rather than reseting all crests.
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HeroController), "ResetWarriorCrestState")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallResetWarriorCrestState(HeroController instance)
    {
        throw new NotImplementedException("Reverse patch not applied for ResetWarriorCrestState");
    }

    // Book keeping method. If rage is reset by the vanilla timeout system, set our budget back to zero
    // and set pending attack back to None
    [HarmonyPatch(typeof(HeroController), "ResetWarriorCrestState")]
    [HarmonyPostfix]
    private static void OnWarriorCrestReset(HeroController __instance)
    {
        Debug.Log("[BeastRework] Rage crest reset. Clearing state");
        attackBudget = 0;
        pendingAttack = AttackType.None;
    }

    // If the rage effect has been reset, set the rageEffectActive flag to false
    [HarmonyPatch(typeof(HeroController), "ResetWarriorRageEffect")]
    [HarmonyPostfix]
    private static void OnRageEffectReset() {
        rageEffectActive = false;
    }

    // A valid hit on an enemy should add additional time to your rage timer
    [HarmonyPatch(typeof(HeroController), "NailHitEnemy")]
    [HarmonyPostfix]
    private static void OnNailHitEnemy(ref HeroController.WarriorCrestStateInfo ___warriorState, HitInstance hitInstance)
    {
        // Increment timer if the player hit and did damage
        if (___warriorState.IsInRageMode && hitInstance.DamageDealt > 0 && rageEffectActive)
        {
            Debug.Log($"[BeastRework] Nail attack hit. Extending rage timeout. Prev | {___warriorState.RageEffectTimeLeft} : New | {RAGE_EFFECT_TIME_CONSTANT}");
            ___warriorState.RageEffectTimeLeft += RAGE_EFFECT_REPLENISHMENT;
            ___warriorState.RageHealTimeLeft += RAGE_HEAL_REPLENISHMENT;
        }

    }

    // If the player takes damage we should decrement their attack budget by 1. It's worth mentioning that in addition to decrementing the
    // attack budget, vanilla behavior is also to decrement your rage effect time by 2.5 seconds.
    [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
    [HarmonyPostfix]
    private static void OnHeroDamaged(int amount)
    {
        Debug.Log("[BeastRework] Hero Damage detected. Grabbing rage instance");
        var instance = HeroController.instance;
        if (!instance.WarriorState.IsInRageMode || attackBudget <= 0 || amount <= 0)
            return;

        attackBudget--;

        Debug.Log($"[BeastRework] Damage penalty | attackBudget={attackBudget}");

        if (attackBudget == 0)
            EndRage(instance);
    }


    [HarmonyPatch(typeof(HeroController), "BindCompleted")]
    [HarmonyPostfix]
    private static void OnBindCompleted(ref HeroController.WarriorCrestStateInfo ___warriorState)
    {
        if (!___warriorState.IsInRageMode) return;
        attackBudget = MAXIMUM_ATTACK_BUDGET;
        rageEffectActive = true;
        Debug.Log($"[BeastRework] Attack attackBudget set to 8");

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
            if (attackBudget == 0)
            {
                Debug.Log("[BeastRework] attackBudget already expired");
                return;
            }

            attackBudget--;
            Debug.Log($"[BeastRework] attackBudget " + attackBudget);

            if (attackBudget == 0)
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

    // This appears to be called when an attack animation ends. If our budget is <= 0 then we want to end rage
    [HarmonyPatch(typeof(HeroController), "ResetAttacks")]
    [HarmonyPostfix]
    private static void OnResetAttacks(HeroController __instance)
    {
        if (__instance.WarriorState.IsInRageMode && attackBudget <= 0 && pendingAttack == AttackType.Slash)
        {
            // Terminate Rage
            Debug.Log("[BeastRework] Reset end attack detected");
            EndRage(__instance);
        }
    }

    // This isn't currently used as the beast downstrike is actually a custom attack that runs through PlayMaker
    // Analysis of how this attack works shows that it goes through approximately 4 animation states before it returns
    // you to idle.
    [HarmonyPatch(typeof(HeroController), "FinishDownspike", new[] { typeof(bool) })]
    [HarmonyPostfix]
    private static void OnFinishDownspike(HeroController __instance)
    {
        if (__instance.WarriorState.IsInRageMode && attackBudget <= 0 && pendingAttack == AttackType.Downspike)
        {
            // Terminate Rage
            Debug.Log("[BeastRework] Downspike end attack detected");
            EndRage(__instance);
        }
    }

    // This method is called for a number of reasons ( animation cancelling, being knocked out of an attack, etc )
    // Most importantly, this method is called at the end of long-lived custom attacks like beast's downstrike.
    // We run a check in this method to determine when we should terminate rage so that final, custom attacks
    // don't terminate your last move before it's had a chance to complete.
    [HarmonyPatch(typeof(HeroController), "CancelAttack", new[] { typeof(bool) })]
    [HarmonyPostfix]
    private static void OnCancelAttack(HeroController __instance) {
        if (__instance.WarriorState.IsInRageMode && attackBudget <= 0)
        {
            Debug.Log("[BeastRework] Cancel end attack detected");
            EndRage(__instance);
        }
    }
}
